@description('The name of the function app that you wish to create.')
param appName string = 'rulesgpt-stage'
param linuxFxVersion string = 'DOTNETCORE|7.0' 

@description('Storage Account type')
@allowed([
  'Standard_LRS'
  'Standard_GRS'
  'Standard_RAGRS'
])
param storageAccountType string = 'Standard_LRS'

@description('Location for all resources.')
param location string = resourceGroup().location

@description('Location for Application Insights')
param appInsightsLocation string = 'australiaeast'

@description('Location for Static Web App')
param staticWebAppLocation string = 'eastasia'

//@description('The language worker runtime to load in the function app.')
//@allowed([
//  'node'
//  'dotnet'
//  'java'
//])
//param runtime string = 'dotnet'

//@allowed([
//  'S0'
//])
//param openaiSku string = 'S0'

@allowed([
  'Free'
  'Standard'
])
param frontendSku string = 'Free'

//var functionAppName = 'azfn-${appName}'
var hostingPlanName = 'plan-${appName}'
var applicationInsightsName = 'ai-${appName}'
var storageAccountName = 'storagerulesgpt'

//var cognitiveServiceName = 'openai-${appName}'
var apiAppName = 'api-${appName}'
var frontendAppName = 'frontend-${appName}'

//var functionWorkerRuntime = runtime

//resource cognitiveService 'Microsoft.CognitiveServices/accounts@2021-10-01' = {
//  name: cognitiveServiceName
//  location: location
//  sku: {
//    name: openaiSku
//  }
//  kind: 'OpenAI'
//  properties: {
//    apiProperties: {
//      statisticsEnabled: false
//    }
//  }
//}

resource storageAccount 'Microsoft.Storage/storageAccounts@2022-05-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: storageAccountType
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

resource appService 'Microsoft.Web/sites@2020-06-01' = {
  name: apiAppName
  location: location
  properties: {
    serverFarmId: hostingPlan.id
    siteConfig: {
      numberOfWorkers: 1
      netFrameworkVersion: 'v7.0'
      alwaysOn: false
      http20Enabled: false
    }
  }
}

resource staticWebApp 'Microsoft.Web/staticSites@2021-01-15' = {
  name: frontendAppName
  location: staticWebAppLocation
  tags: null
  properties: {}
  sku: {
      name: frontendSku
      size: frontendSku
  }
}

//resource functionApp 'Microsoft.Web/sites@2021-03-01' = {
//  name: functionAppName
//  location: location
//  kind: 'functionapp'
//  identity: {
//    type: 'SystemAssigned'
//  }
//  properties: {
//    serverFarmId: hostingPlan.id
//  }
//}

resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: applicationInsightsName
  location: appInsightsLocation
  kind: 'web'
  properties: {
    Application_Type: 'web'
    Request_Source: 'rest'
  }
}
