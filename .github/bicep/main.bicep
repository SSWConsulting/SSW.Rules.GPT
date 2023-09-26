param appName string = 'rulesgpt-stage'

@secure()
param connectionString string = ''
@secure()
param openAiApiKey string = ''

param allowedCors string = ''
param maxRequests string = ''
param signingAuthority string = ''

@description('Specifies the object ID of a user, service principal or security group in the Azure Active Directory tenant for the vault. The object ID must be unique for the list of access policies. Get it by using Get-AzADUser or Get-AzADServicePrincipal cmdlets.')
param objectId string = ''

param location string = resourceGroup().location
param staticWebAppLocation string = 'eastasia'

//Can't contain uppercase letters or special characters
var storageAccountName = 'storerulesgpt'
var hostingPlanName = 'plan-${appName}'
var keyVaultName = 'kv-${appName}'
var tenantId = subscription().tenantId

var apiAppName = 'api-${appName}'
var frontendAppName = 'frontend-${appName}'

var applicationInsightsName = 'ai-${appName}'

resource storageAccount 'Microsoft.Storage/storageAccounts@2022-05-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'Storage'
  properties: {
    supportsHttpsTrafficOnly: true
    defaultToOAuthAuthentication: true
  }
}

resource hostingPlan 'Microsoft.Web/serverfarms@2021-03-01' = {
  name: hostingPlanName
  location: location
  sku: {
    name: 'B1'
    tier: 'Dynamic'
  }
  properties: {}
  kind: 'linux'
}

resource kv 'Microsoft.KeyVault/vaults@2021-11-01-preview' = {
  name: keyVaultName
  location: location
  properties: {
    enabledForDeployment: false
    enabledForDiskEncryption: false
    enabledForTemplateDeployment: false
    tenantId: tenantId
    enableSoftDelete: true
    softDeleteRetentionInDays: 90
    accessPolicies: [
      {
        objectId: objectId
        tenantId: tenantId
        permissions: {
          keys: [ 'get', 'list' ]
          secrets: [ 'get', 'list' ]
        }
      }
    ]
    sku: {
      name: 'standard'
      family: 'A'
    }
    networkAcls: {
      defaultAction: 'Allow'
      bypass: 'AzureServices'
    }
  }
}

resource dbSecret 'Microsoft.KeyVault/vaults/secrets@2021-11-01-preview' = {
  parent: kv
  name: 'ConnectionStrings--DefaultConnection'
  properties: {
    value: connectionString
  }
}

resource openaiApiKeySecret 'Microsoft.KeyVault/vaults/secrets@2021-11-01-preview' = {
  parent: kv
  name: 'OpenAiApiKey'
  properties: {
    value: openAiApiKey
  }
}

resource backendAppService 'Microsoft.Web/sites@2020-06-01' = {
  name: apiAppName
  location: location
  properties: {
    serverFarmId: hostingPlan.id
    siteConfig: {
      numberOfWorkers: 1
      netFrameworkVersion: 'v7.0'
      alwaysOn: false
      http20Enabled: false
      appSettings: [
        {
          name: 'AllowedCORSOrigins'
          value: allowedCors
        }
        {
          name: 'MaxRequestsPerMinute'
          value: maxRequests
        }
        {
          name: 'SigningAuthority'
          value: signingAuthority
        }
        {
          name: 'ConnectionStrings__DefaultConnection'
          value: '@Microsoft.KeyVault(SecretUri=${dbSecret.properties.secretUri})'
        }
        {
          name: 'OpenAiApiKey'
          value: '@Microsoft.KeyVault(SecretUri=${openaiApiKeySecret.properties.secretUri})'
        }
        {
          name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
          value: applicationInsights.properties.InstrumentationKey
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: applicationInsights.properties.ConnectionString
        }
      ]
    }
  }
}

resource frontendStaticWebApp 'Microsoft.Web/staticSites@2021-01-15' = {
  name: frontendAppName
  location: staticWebAppLocation
  tags: null
  properties: {}
  sku: {
    name: 'Standard'
    size: 'Standard'
  }
}

//TODO: Add Log Analytics - this will stop working in february 2024
resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: applicationInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    Request_Source: 'rest'
  }
}
