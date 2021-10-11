$resourceGroupName = "rsg-CosmosDb"
$accountName = "ir77-cosmosdb"
$roleDefinitionId = "00000000-0000-0000-0000-000000000001"
$principalId = "3f0a14f9-711d-4ab3-b4ec-d5c0928534df"
New-AzCosmosDBSqlRoleAssignment -AccountName $accountName -ResourceGroupName $resourceGroupName -RoleDefinitionId $roleDefinitionId -Scope "/" -PrincipalId $principalId


$AssignedRole = Get-AzCosmosDBSqlRoleAssignment -ResourceGroupName $resourceGroupName -AccountName $accountName
Remove-AzCosmosDBSqlRoleAssignment -ResourceGroupName $resourceGroupName -AccountName $accountName -Id $AssignedRole.Id -PassThru true
