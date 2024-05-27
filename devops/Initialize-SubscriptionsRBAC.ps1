function Initialize-SubscriptionsRBAC {
    [CmdletBinding(SupportsShouldProcess=$true)]
    param (
        [parameter(Mandatory = $true, Position = 0)]
        [string] $RuleFilePath,
        [parameter(Mandatory = $true, Position = 1)]
        [string] $principalId
    )

    $rules = Get-Content $RuleFilePath | ConvertFrom-Json
    $subIds = $rules.Subscriptions | Select-Object -ExpandProperty SubscriptionId

    $templateFile = Join-Path $PSScriptRoot -ChildPath ".\costAPISubRBAC.bicep"

    foreach ($subId in $subIds)
    {
        Select-AzSubscription -SubscriptionId $subId

        New-AzSubscriptionDeployment -Name "CostManagementReaderDevopsAKSPrincipal" `
                                     -Location northeurope `
                                     -TemplateFile $templateFile `
                                     -principalId $principalId `
                                     -Verbose:$PSCmdlet.MyInvocation.BoundParameters['Verbose'].IsPresent
    }
}