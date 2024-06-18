targetScope = 'subscription'

param principalId string

var costManagementReaderRoleId = '72fafb9e-0641-4937-9268-a91bfd8191a3' //CostManagementReader
var logAnalyticsReaderRoleId = '73c42c96-874c-492b-b04d-ab87d138a893' //LogAnalyticsReader

resource subCostApiAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(costManagementReaderRoleId, principalId, subscription().id)
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', costManagementReaderRoleId)
    principalId: principalId
    principalType: 'ServicePrincipal'
  }
}

resource subLogAnalyticsWorkspaceReaderAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(logAnalyticsReaderRoleId, principalId, subscription().id)
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', logAnalyticsReaderRoleId)
    principalId: principalId
    principalType: 'ServicePrincipal'
  }
}
