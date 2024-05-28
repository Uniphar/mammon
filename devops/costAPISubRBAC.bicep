targetScope = 'subscription'

param principalId string

var roleId = '72fafb9e-0641-4937-9268-a91bfd8191a3' //CostManagementReader

resource subCostApiAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(roleId, principalId, subscription().id)
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roleId)
    principalId: principalId
    principalType: 'ServicePrincipal'
  }
}
