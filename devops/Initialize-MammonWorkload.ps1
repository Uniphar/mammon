<#
.SYNOPSIS
Initializes the Mammon workload in the specified environment.

.DESCRIPTION
This script deploys the Mammon workload using a Bicep template. It requires the environment to be specified as either 'dev', 'test', or 'prod'. The script retrieves necessary resources such as Key Vault and Redis Cache, and performs the deployment or validation of the deployment.

.PARAMETER Environment
Specifies the environment for which the Mammon workload should be initialized. Valid values are 'dev', 'test', and 'prod'.

.EXAMPLE
Initialize-MammonWorkload -Environment 'dev'
This command initializes the Mammon workload in the development environment.

.NOTES
redis secret part ought to be replaced by key rotation tool when it is available for redis.

#>
function Initialize-MammonWorkload {
    [CmdletBinding(SupportsShouldProcess = $true)]
    param (
        [parameter(Mandatory = $true, Position = 0)]
        [ValidateSet('dev', 'test', 'prod')]
        [string] $Environment 
    )

    $templateFile = Join-Path $PSScriptRoot -ChildPath '.\domain.bicep'

    $devopsAppKeyVault = Resolve-UniResourceName 'keyvault' "$p_devopsDomain-app" -Environment $Environment
    $devopsDomainRgName = Resolve-UniResourceName 'resource-group' $p_devopsDomain -Environment $Environment

    $redisRG = Resolve-UniResourceName 'resource-group' $p_dataRedis -Environment $Environment
    $redisName = Resolve-UniResourceName 'redis-cache' $p_dataRedis -Environment $Environment

    $redis = Get-AZredisCache -ResourceGroupName $redisRG -Name $redisName -ErrorAction Stop

    $redisKeys = Get-AzRedisCacheKey -ResourceGroupName $redisRG -Name $redisName -ErrorAction Stop

     if ($PSCmdlet.ShouldProcess('Mammon', 'Deploy')) {

        $deploymentName = Resolve-DeploymentName -Suffix '-mammon'

        New-AzResourceGroupDeployment -Mode Incremental `
                                      -Name $deploymentName `
                                      -ResourceGroupName $devopsDomainRgName `
                                      -TemplateFile $templateFile `
                                      -kvName $devopsAppKeyVault `
                                      -redisConnectionString "$($redis.HostName):$($redis.SslPort)" `
                                      -redisSecret $redisKeys.PrimaryKey `
                                      -Verbose:$PSCmdlet.MyInvocation.BoundParameters['Verbose'].IsPresent
     }
     else {
        $TestResult = Test-AzResourceGroupDeployment -Mode Incremental `
                                                     -ResourceGroupName $devopsDomainRgName `
                                                     -TemplateFile $templateFile `
                                                     -kvName $devopsAppKeyVault `
                                                     -redisConnectionString "$($redis.HostName):$($redis.SslPort)" `
                                                     -redisSecret $redisKeys.PrimaryKey `
                                                     -Verbose:$PSCmdlet.MyInvocation.BoundParameters['Verbose'].IsPresent

         if ($TestResult) {
            $TestResult
            throw "The deployment for $devopsDomainTemplateFile did not pass validation."
        }
     }
}