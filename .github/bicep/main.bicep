param appName string = 'rulesgpt-stage'
param environmentVariables array = []

@secure()
param connectionString string = ''

@secure()
param openAiApiKey string = ''

@description('Specifies the object ID of a user, service principal or security group in the Azure Active Directory tenant for the vault. The object ID must be unique for the list of access policies. Get it by using Get-AzADUser or Get-AzADServicePrincipal cmdlets.')
param objectId string = ''

@description('Location for all resources.')
param location string = resourceGroup().location

@description('Location for Application Insights')
param appInsightsLocation string = 'australiaeast'

@description('Location for Static Web App')
param staticWebAppLocation string = 'eastasia'

var storageAccountName = 'store-${appName}'
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
          keys: [ 'list' ]
          secrets: [ 'list' ]
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
  name: 'ConnectionStrings__DefaultConnection'
  properties: {
    value: connectionString
  }
}

resource apiSecret 'Microsoft.KeyVault/vaults/secrets@2021-11-01-preview' = {
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
      appSettings: environmentVariables
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

resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: applicationInsightsName
  location: appInsightsLocation
  kind: 'web'
  properties: {
    Application_Type: 'web'
    Request_Source: 'rest'
  }
}
