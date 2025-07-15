<#
.SYNOPSIS
Test connection to a vCenter server using provided credentials.

.PARAMETER vCenterServer
The vCenter server address or hostname.

.PARAMETER Credential
A PSCredential object containing username and password.

.EXAMPLE
$cred = Get-Credential
.\Test-VCConnection.ps1 -vCenterServer "vcenter01.domain.com" -Credential $cred
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$vCenterServer,

    [Parameter(Mandatory = $true)]
    [System.Management.Automation.PSCredential]$Credential
)

try {
    # Import VMware PowerCLI module if not already loaded
    if (-not (Get-Module -Name VMware.VimAutomation.Core)) {
        Import-Module VMware.VimAutomation.Core -ErrorAction Stop
    }

    # Connect to vCenter
    $connection = Connect-VIServer -Server $vCenterServer -Credential $Credential -ErrorAction Stop

    # Retrieve vCenter version info
    $version = $connection.Version

    # Disconnect session
    Disconnect-VIServer -Server $vCenterServer -Confirm:$false

    # Output success message with version
    Write-Output "Connected successfully"
    Write-Output "Version: $version"
}
catch {
    # Output failure message with error details
    Write-Output "Connection failed: $($_.Exception.Message)"
    exit 1
}
