[CmdletBinding(DefaultParameterSetName = 'Check')]
param(
    [Parameter(ParameterSetName = 'Check')]
    [switch] $Check,

    [Parameter(Mandatory = $true, ParameterSetName = 'Update')]
    [switch] $Update,

    [Parameter(Mandatory = $true, ParameterSetName = 'Install')]
    [switch] $Install,

    [Parameter(ParameterSetName = 'Check')]
    [Parameter(ParameterSetName = 'Update')]
    [Parameter(ParameterSetName = 'Install')]
    [string] $Ref,

    [Parameter(Mandatory = $true, ParameterSetName = 'Restore')]
    [string] $RestoreBackup,

    [string] $Python = 'python'
)

$ErrorActionPreference = 'Stop'
$command = Get-Command $Python -CommandType Application | Select-Object -First 1
$helper = Join-Path $PSScriptRoot 'ppt_master_maintenance.py'
$mode = $PSCmdlet.ParameterSetName.ToLowerInvariant()
$arguments = @($helper, $mode)
if ($Ref) { $arguments += @('--ref', $Ref) }
if ($RestoreBackup) { $arguments += @('--backup', $RestoreBackup) }

& $command.Source -B @arguments
exit $LASTEXITCODE
