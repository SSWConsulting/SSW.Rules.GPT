param appName string = 'rulesgpt'
param environment string = 'stage'

@secure()
param connectionString string
@secure()
param openAiApiKey string

param allowedCors string
param maxRequests string
param signingAuthority string

param location string = resourceGroup().location
param staticWebAppLocation string = 'eastasia'

param hostingPlanName string 
param hostingPlanRgName string

var prodEnvironmentName = environment == 'prod' ? '' : '-${environment}'

//Can't contain uppercase letters or special characters
var storageAccountName = toLower(take(replace('sa${appName}${environment}', '-', ''), 24))
var keyVaultName = 'kv-${appName}-${environment}'
var tenantId = subscription().tenantId

var apiAppName = 'ssw-${appName}-api${prodEnvironmentName}'
var frontendAppName = 'ssw-${appName}-webui${prodEnvironmentName}'
var applicationInsightsName = 'ai-${appName}-${environment}'


var lawName = 'laws-${appName}${prodEnvironmentName}'

resource storageAccount 'Microsoft.Storage/storageAccounts@2022-05-01' = {
#disable-next-line BCP334
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

resource hostingPlan 'Microsoft.Web/serverfarms@2021-03-01' existing = {
  name: hostingPlanName
  scope: resourceGroup(hostingPlanRgName)
}

resource kv 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  properties: {
    accessPolicies: []
    enabledForDeployment: false
    enabledForDiskEncryption: false
    enabledForTemplateDeployment: true
    tenantId: tenantId
    enableSoftDelete: true
    softDeleteRetentionInDays: 90
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

resource backendAppService 'Microsoft.Web/sites@2020-12-01' = {
  name: apiAppName
  location: location
  kind: 'app,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: hostingPlan.id
    reserved: true
    siteConfig: {
      numberOfWorkers: 1
      linuxFxVersion: 'DOTNETCORE|8.0'
      alwaysOn: false
      http20Enabled: false
      cors: {
        allowedOrigins: [ allowedCors ]
      }
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

resource keyVaultAccessPolicy 'Microsoft.KeyVault/vaults/accessPolicies@2023-07-01' = {
  parent: kv
  name: 'add'
  properties: {
    accessPolicies: [
      {
        objectId: backendAppService.identity.principalId
        tenantId: backendAppService.identity.tenantId
        permissions: {
          secrets: [
            'list'
            'get'
          ]
          keys: [
            'list'
            'get'
          ]
        }
      }
    ]
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

resource law 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: lawName
  location: location
  properties: {
    retentionInDays: 30
    features: {
      searchVersion: 1
    }
    sku: {
      name: 'PerGB2018'
    }
  }
}

resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: applicationInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    Request_Source: 'rest'
    WorkspaceResourceId: law.id
  }
}
