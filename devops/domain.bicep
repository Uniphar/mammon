param kvName string
param redisConnectionString string
param redisSecret string

param location string = resourceGroup().location

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01'  existing = {
  name: kvName 

  resource redisConnectionResource 'secrets@2023-07-01' = {
    name: 'REDIS--CONNECTIONSTRING'
    properties: {
      value: redisConnectionString
    }
  }  

  resource redisSecretResource 'secrets@2023-07-01' = {
    name: 'REDIS--KEY'
    properties: {
      value: redisSecret
    }
  }
}
