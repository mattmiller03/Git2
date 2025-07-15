<#
.SYNOPSIS
Tests connection to a vCenter server

.PARAMETER vCenterServer
The vCenter server address

.PARAMETER Credential
PSCredential containing username/password

.PARAMETER Environment
Environment tag (TEST/PROD)

.EXAMPLE
Test-VCenterConnection -vCenterServer "vc01.example.com" -Credential $cred -Environment "TEST"
#>
param(
    [Parameter(Mandatory=$true)]
    [string]$vCenterServer,
    
    [Parameter(Mandatory=$true)]
    [System.Management.Automation.PSCredential]$Credential,
    
    [string]$Environment = "TEST"
)

try {
    # Connect to vCenter
    Connect-VIServer -Server $vCenterServer -Credential $Credential -ErrorAction Stop
    
    # Get server info
    $serverInfo = Get-VIAccount
    $version = (Get-VIAccount).ServerVersion
    
    # Return success
    "Connected successfully to $vCenterServer`nVersion: $version`nEnvironment: $Environment"
}
catch {
    "Connection failed: $_"
}
finally {
    if ($global:DefaultVIServers) {
        Disconnect-VIServer -Server * -Confirm:$false
    }
}
