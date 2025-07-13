#region A) PARAMETERS
[CmdletBinding()]
Param(
    [Parameter(Mandatory=$true, HelpMessage="vCenter Server name or IP")]
    [string]$vCenterServer,

    [Parameter(Mandatory=$true, HelpMessage="Credential object for vCenter and SSO")]
    [pscredential]$Credential,

    [Parameter(Mandatory=$true, HelpMessage="Path to the Excel file containing tag and permission data")]
    [string]$ExcelFilePath,

    # <-- MODIFIED: The Environment parameter is back, now used as a safety check.
    [Parameter(Mandatory=$true, HelpMessage="The target environment (e.g., DEV, PROD). Must match the selection in the Excel file.")]
    [ValidateNotNullOrEmpty()]
    [string]$Environment,

    [Parameter(HelpMessage="Enable detailed script debug logging")]
    [switch]$EnableScriptDebug
)

# Global variables for logging and connection status
$global:outputLog = @()
$global:logFolder = Join-Path (Split-Path $MyInvocation.MyCommand.Path) "Logs"
$global:ssoConnected = $false
$global:ScriptDebugEnabled = $false

# <-- MODIFIED: The entire $EnvironmentCategoryConfig hashtable has been removed.
# This configuration now lives entirely within the Excel file's 'Config' sheet
# and is interpreted by Excel formulas before the script even runs.

# Static mapping of OS patterns to target TagNames (This logic remains the same)
<# $StaticOSPatterns = @{
    # Windows Server
    "Microsoft Windows Server 2012.*" = "Windows-server";
    "Microsoft Windows Server 2016.*" = "Windows-server";
    "Microsoft Windows Server 2019.*" = "Windows-server";
    "Microsoft Windows Server 2022.*" = "Windows-server";
    "Microsoft Windows Server.*" = "Windows-server";
    # Windows Client
    "Microsoft Windows 10.*" = "Windows-client";
    "Microsoft Windows 11.*" = "Windows-client";
    "Microsoft Windows.*" = "Windows-client";
    # Linux
    "Ubuntu Linux.*" = "Ubuntu Linux";
    "CentOS Linux.*" = "CentOS Linux";
    "Red Hat Enterprise Linux.*" = "RHEL";
    "SUSE Linux Enterprise Server.*" = "SLES";
    "VMware Photon OS.*" = "Photon OS";
    ".*Linux.*" = "Linux";
    # Other
    "VMware ESXi.*" = "ESXi";
} #>
#endregion

#region B) LOGGING
# Ensure log folder exists
if (-not (Test-Path $global:logFolder)) {
    try {
        New-Item -Path $global:logFolder -ItemType Directory -Force | Out-Null
        # Fallback log for this specific action if Write-Log isn't ready
        Write-Host "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') [INFO ] Log folder created: $($global:logFolder)" -ForegroundColor Green
    }
    catch {
        Write-Host "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') [ERROR] Failed to create log folder $($global:logFolder): $_" -ForegroundColor Red
        # If log folder creation fails, subsequent logging might also fail.
        # The script will continue, but logs will only go to console.
    }
}

# Fallback logging function if main logging fails or isn't initialized
function Write-FallbackLog {
    [CmdletBinding()]
    Param(
        [Parameter(Mandatory=$true)]
        [string]$Message
    )
    # Simple console output with timestamp and ERROR level
    Write-Host "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') [ERROR] (Fallback) $($Message)" -ForegroundColor Red
}

# Main logging function
function Write-Log {
    [CmdletBinding()]
    Param(
        [Parameter(Mandatory=$true)]
        [string]$Message,

        [Parameter(Mandatory=$false)]
        [ValidateSet("INFO", "WARN", "ERROR", "DEBUG")]
        [string]$Level = "INFO"
    )

    # Determine if this log level should be written
    $writeThisLog = $false
    switch ($Level.ToUpper()) {
        "INFO" { $writeThisLog = $true }
        "WARN" { $writeThisLog = $true }
        "ERROR" { $writeThisLog = $true }
        "DEBUG" {
            # Check the script's specific debug flag set by the -EnableScriptDebug parameter
            if ($global:ScriptDebugEnabled) {
                 $writeThisLog = $true
            }
        }
    }

    if ($writeThisLog) {
        $logEntry = [PSCustomObject]@{
            Timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
            Level     = $Level.ToUpper()
            Message   = $Message
        }
        # Add to in-memory log
        $global:outputLog += $logEntry

        # Write to host for immediate feedback (optional, but good for debugging)
        $hostColor = switch ($Level.ToUpper()) {
            "INFO" { "Green" }
            "WARN" { "Yellow" }
            "ERROR" { "Red" }
            "DEBUG" { "Gray" }
            Default { "White" }
        }
        Write-Host "$($logEntry.Timestamp) [$($logEntry.Level.PadRight(5))] $($logEntry.Message)" -ForegroundColor $hostColor
    }
}

# Function to clean up old log files
function Cleanup-OldLogs {
    [CmdletBinding()]
    Param(
        [Parameter(Mandatory=$true)]
        [string]$LogFolder,

        [Parameter(Mandatory=$true)]
        [int]$MaxLogsToKeep
    )

    Write-Log "Cleaning up old logs in '$($LogFolder)', keeping $($MaxLogsToKeep) most recent." "DEBUG"

    try {
        # Get log files, sort by last write time (newest first)
        $logFiles = Get-ChildItem -Path $LogFolder -Filter "*.log" -File | Sort-Object LastWriteTime -Descending

        # Identify files to remove (all except the newest $MaxLogsToKeep)
        if ($logFiles.Count -gt $MaxLogsToKeep) {
            $filesToRemove = $logFiles | Select-Object -Skip $MaxLogsToKeep

            Write-Log "Found $($filesToRemove.Count) old log files to remove." "DEBUG"

            foreach ($file in $filesToRemove) {
                Write-Log "Removing old log file: $($file.FullName)" "DEBUG"
                try {
                    Remove-Item -Path $file.FullName -Force -ErrorAction Stop
                    Write-Log "  Successfully removed $($file.Name)." "DEBUG"
                }
                catch {
                    Write-Log "  Failed to remove old log file $($file.FullName): $_" "WARN"
                }
            }
        } else {
            Write-Log "No old log files to remove (found $($logFiles.Count), keeping $($MaxLogsToKeep))." "DEBUG"
        }
    }
    catch {
        Write-Log "Error during log cleanup: $_" "WARN"
        Write-Log "  Stack trace: $($_.ScriptStackTrace)" "DEBUG"
    }
}

Write-Log "Script started." "INFO"
Write-Log "PowerShell Version: $($PSVersionTable.PSVersion)" "DEBUG"
Write-Log "PowerCLI Module Info:" "DEBUG"
try {
    Get-Module -Name VMware.PowerCLI -ErrorAction Stop | Select-Object Name, Version, Path | Format-List | Out-String | ForEach-Object { Write-Log "  $_" "DEBUG" }
}
catch {
    Write-Log "  VMware.PowerCLI module not found or error getting info: $_" "WARN"
}

# Initial log cleanup (before main execution starts)
Cleanup-OldLogs -LogFolder $global:logFolder -MaxLogsToKeep 5
#endregion

#region D) SSO HELPER FUNCTIONS

function Test-SsoModuleAvailable {
    Write-Log "Checking for VMware.vSphere.SsoAdmin module..." "DEBUG"
    try {
        # Use -ListAvailable to avoid loading the module just for the check
        $module = Get-Module -Name VMware.vSphere.SsoAdmin -ListAvailable -ErrorAction Stop
        if ($module) {
            Write-Log "  VMware.vSphere.SsoAdmin module found." "DEBUG"
            # Attempt to import the module if found but not loaded
            if (-not (Get-Module -Name VMware.vSphere.SsoAdmin)) {
                 Write-Log "  Importing VMware.vSphere.SsoAdmin module..." "DEBUG"
                 Import-Module VMware.vSphere.SsoAdmin -ErrorAction Stop | Out-Null
                 Write-Log "  Module imported." "DEBUG"
            }
            return $true
        } else {
            Write-Log "  VMware.vSphere.SsoAdmin module not found." "DEBUG"
            return $false
        }
    } catch {
        Write-Log "  Error checking/importing SSO module: $_" "WARN"
        Write-Log "  Stack trace: $($_.ScriptStackTrace)" "DEBUG"
        return $false
    }
}

function Connect-SsoAdmin {
    param(
        [Parameter(Mandatory=$true)]
        [string]$Server,
        [Parameter(Mandatory=$true)]
        [System.Management.Automation.PSCredential]$Credential
    )
    Write-Log "Attempting to connect to SSO Admin server '$($Server)'..." "DEBUG"
    $global:ssoConnected = $false # Assume failure initially

    try {
        # Connect to the SSO Admin server
        # This cmdlet handles the connection based on the vCenter server address
        Connect-SsoAdminServer -Server $Server -Credential $Credential -ErrorAction Stop | Out-Null
        $global:ssoConnected = $true
        Write-Log "  Successfully connected to SSO Admin server." "DEBUG"
        return $true

    } catch {
        Write-Log "  Failed to connect to SSO Admin server '$($Server)': $_" "ERROR"
        Write-Log "  Stack trace: $($_.ScriptStackTrace)" "DEBUG"
        $global:ssoConnected = $false
        return $false
    }
}

function Test-SsoGroupExistsSimple {
    param(
        [Parameter(Mandatory=$true)]
        [string]$Domain,
        [Parameter(Mandatory=$true)]
        [string]$GroupName
    )
    # Ensure SSO is connected before attempting to check
    if (-not $global:ssoConnected) {
        Write-Log "    SSO is not connected. Cannot check existence of group '$($Domain)\$($GroupName)'." "WARN"
        return $false
    }

    # Construct the principal name in the format expected by Get-SsoGroup
    # Get-SsoGroup expects "GroupName@Domain" for domain groups
    $principalName = "$GroupName@$Domain"
    Write-Log "    Checking SSO group existence for principal name '$($principalName)'..." "DEBUG"

    try {
        # Attempt to retrieve the group. If it doesn't exist, Get-SsoGroup throws an error.
        # Using -ErrorAction Stop inside try/catch is the standard way to handle this.
        $ssoGroup = Get-SsoGroup -Name $GroupName -Domain $Domain -ErrorAction Stop

        # If we reach here, the group was found
        Write-Log "    SSO group '$($principalName)' found." "DEBUG"
        return $true

    } catch {
        # If an error occurred, the group was not found or there was another SSO issue
        Write-Log "    SSO group '$($principalName)' not found or check failed: $($_.Exception.Message)" "ERROR"
        Write-Log "    Stack trace: $($_.ScriptStackTrace)" "DEBUG"
        return $false
    }
}

#endregion

#region E) HELPER FUNCTIONS

function Get-ValueNormalized {
    param(
        [pscustomobject]$Row,
        [string]$ColumnName
    )
    # Attempt to get value by exact column name first
    $value = $null
    if ($Row.PSObject.Properties.Match($ColumnName).Count -gt 0) {
        $value = $Row.$ColumnName
    } else {
        # If not found, try case-insensitive match
        $prop = $Row.PSObject.Properties | Where-Object { $_.Name -ieq $ColumnName } | Select-Object -First 1
        if ($prop) {
            $value = $prop.Value
        }
    }

    # Handle potential null or empty values and trim whitespace
    if ($value -is [string]) {
        return $value.Trim()
    } elseif ($value -ne $null) {
        # Convert non-string values to string, then trim
        return ($value.ToString()).Trim()
    } else {
        return "" # Return empty string for null values
    }
}

function Get-TagCategoryIfExists {
    param([string]$CategoryName)
    Write-Log "  Checking if tag category '$($CategoryName)' exists (case-insensitive)..." "DEBUG" # Updated log message
    try {
        # Get all categories and filter by name case-insensitively
        $cat = Get-TagCategory -ErrorAction SilentlyContinue | Where-Object { $_.Name -ieq $CategoryName } | Select-Object -First 1
        if ($cat) {
            Write-Log "    Category '$($CategoryName)' found (case-insensitive match)." "DEBUG" # Updated log message
            return $cat
        } else {
            Write-Log "    Category '$($CategoryName)' not found." "DEBUG"
            return $null
        }
    } catch {
        Write-Log "    Error checking for category '$($CategoryName)': $_" "WARN"
        return $null
    }
}

function Ensure-TagCategory {
    param(
        [string]$CategoryName,
        [string]$Description = "Managed by script",
        [string]$Cardinality = "MULTIPLE", # "SINGLE", "MULTIPLE"
        [string[]]$EntityType = @("VirtualMachine") # e.g., @("VirtualMachine", "Datastore")
    )
    Write-Log "  Ensuring tag category '$($CategoryName)' exists (case-insensitive check)..." "DEBUG" # Updated log message

    # Check if category already exists (case-insensitively)
    try {
        $existingCat = Get-TagCategory -ErrorAction SilentlyContinue | Where-Object { $_.Name -ieq $CategoryName } | Select-Object -First 1

        if ($existingCat) {
            Write-Log "    Category '$($CategoryName)' already exists (case-insensitive match)." "DEBUG" # Updated log message
            return $existingCat
        }
    } catch {
        Write-Log "    Error checking for existing category '$($CategoryName)': $_" "WARN"
        # Continue to attempt creation if check fails
    }


    # If category doesn't exist, create it
    Write-Log "    Category '$($CategoryName)' not found, creating..." "DEBUG"
    try {
        # Use the exact case from $CategoryName for creation
        $newCat = New-TagCategory -Name $CategoryName -Description $Description -Cardinality $Cardinality -EntityType $EntityType -ErrorAction Stop
        Write-Log "    Category '$($CategoryName)' created successfully." "DEBUG"
        return $newCat
    } catch {
        Write-Log "    Failed to create category '$($CategoryName)': $_" "ERROR"
        Write-Log "    Error details: $($_.Exception.Message)" "DEBUG"
        return $null
    }
}

function Get-TagIfExists {
    param(
        [string]$TagName,
        [string]$CategoryName
    )
    Write-Log "  Checking if tag '$($TagName)' exists in category '$($CategoryName)' (case-insensitive)..." "DEBUG" # Updated log message
    try {
        # Get the category object first (using the case-insensitive helper)
        $cat = Get-TagCategoryIfExists -CategoryName $CategoryName
        if ($cat) {
            # Get all tags in the category and filter by name case-insensitively
            $tag = Get-Tag -Category $cat -ErrorAction SilentlyContinue | Where-Object { $_.Name -ieq $TagName } | Select-Object -First 1
            if ($tag) {
                Write-Log "    Tag '$($TagName)' found (case-insensitive match)." "DEBUG" # Updated log message
                return $tag
            } else {
                Write-Log "    Tag '$($TagName)' not found in category '$($CategoryName)'." "DEBUG"
                return $null
            }
        } else {
            Write-Log "    Category '$($CategoryName)' not found." "DEBUG"
            return $null
        }
    } catch {
        Write-Log "    Error checking for tag '$($TagName)' in category '$($CategoryName)': $_" "WARN"
        return $null
    }
}

function Ensure-Tag {
    param(
        [string]$TagName,
        [VMware.VimAutomation.ViCore.Types.V1.Tagging.TagCategory]$Category # Accept category object
    )
    Write-Log "  Ensuring tag '$($TagName)' exists in category '$($Category.Name)' (case-insensitive check)..." "DEBUG" # Updated log message

    # Check if tag already exists (case-insensitively)
    try {
        # Get all tags in the category and filter by name case-insensitively
        $existingTag = Get-Tag -Category $Category -ErrorAction SilentlyContinue | Where-Object { $_.Name -ieq $TagName } | Select-Object -First 1

        if ($existingTag) {
            Write-Log "    Tag '$($TagName)' already exists (case-insensitive match)." "DEBUG" # Updated log message
            return $existingTag
        }
    } catch {
         Write-Log "    Error checking for existing tag '$($TagName)' in category '$($Category.Name)': $_" "WARN"
         # Continue to attempt creation if check fails
    }

    # If tag doesn't exist, create it
    Write-Log "    Tag '$($TagName)' not found, creating..." "DEBUG"
    try {
        # Use the exact case from $TagName for creation
        $newTag = New-Tag -Name $TagName -Category $Category -Description "Managed by script" -ErrorAction Stop
        Write-Log "    Tag '$($TagName)' created successfully." "DEBUG"
        return $newTag
    } catch {
        Write-Log "    Failed to create tag '$($TagName)' in category '$($Category.Name)': $_" "ERROR"
        Write-Log "    Error details: $($_.Exception.Message)" "DEBUG"
        return $null
    }
}

#endregion

#region C) EXCEL IMPORT FUNCTION
function Import-ExcelCOM {
    param(
        [Parameter(Mandatory=$true)]
        [string]$Path,
        [Parameter(Mandatory=$true)]
        [string]$WorksheetName,
        [Parameter(Mandatory=$false)]
        [string]$ValidationCell,
        # <-- MODIFIED: Added parameter to specify which row contains the headers.
        [Parameter(Mandatory=$true)]
        [int]$HeaderRow
    )

    Write-Log "Attempting to import data from Excel file: $($Path) | Sheet: $($WorksheetName)" "DEBUG"

    if (-not (Test-Path $Path)) {
        Write-Log "Excel file not found at path: $($Path)" "ERROR"
        throw "Excel file not found."
    }

    $excel = $null
    try {
        $excel = New-Object -ComObject Excel.Application
        $excel.Visible = $false
        $excel.DisplayAlerts = $false
        $workbook = $excel.Workbooks.Open($Path)
        $worksheet = $workbook.Sheets.Item($WorksheetName)
        
        if (-not $worksheet) { throw "Worksheet '$($WorksheetName)' not found in the Excel file." }
        Write-Log "  Workbook opened. Using sheet: $($worksheet.Name)." "DEBUG"

       # Read the environment value if a validation cell is provided
        $environmentInExcel = $null
        if (-not [string]::IsNullOrWhiteSpace($ValidationCell)) { # <-- MODIFIED
            $environmentInExcel = $worksheet.Range($ValidationCell).Value2
            Write-Log "  Read environment value from cell $($ValidationCell): '$($environmentInExcel)'" "DEBUG"
        }

        # Find the used range
        $usedRange = $worksheet.UsedRange
        $totalRows = $usedRange.Rows.Count
        $totalCols = $usedRange.Columns.Count
        Write-Log "  Used range found: $($totalRows) rows, $($totalCols) columns." "DEBUG"

        if ($totalRows -lt $HeaderRow) {
            throw "HeaderRow ($($HeaderRow)) is greater than the total number of rows ($($totalRows)) in the sheet."
        }

        # Get headers from the specified header row
        $headerValues = $usedRange.Rows.Item($HeaderRow)
        $headers = @()
        for ($j = 1; $j -le $totalCols; $j++) {
            $header = $headerValues.Cells.Item(1, $j).Value2
            if ([string]::IsNullOrWhiteSpace($header)) {
                # This handles trailing empty columns that might be in the UsedRange
                Write-Log "    Warning: Empty header found in column $j. It will be ignored." "WARN"
                continue
            }
            # Sanitize headers to make them valid property names
            $headers += ($header -replace '[^a-zA-Z0-9_]', '')
        }
        Write-Log "  Headers identified from Row $($HeaderRow): $($headers -join ', ')" "DEBUG"

        # Get data rows, starting from the row immediately after the header row
        $data = @()
        $dataStartRow = $HeaderRow + 1
        for ($i = $dataStartRow; $i -le $totalRows; $i++) {
            $rowObject = New-Object pscustomobject
            $dataRow = $usedRange.Rows.Item($i)
            $isRowEmpty = $true # Flag to skip completely empty rows

            for ($j = 1; $j -le $headers.Count; $j++) { # Loop only for the number of actual headers found
                $cellValue = $dataRow.Cells.Item(1, $j).Value2
                if (-not ([string]::IsNullOrWhiteSpace($cellValue))) {
                    $isRowEmpty = $false
                }
                $rowObject | Add-Member -Type NoteProperty -Name $headers[$j-1] -Value $cellValue -Force
            }

            if (-not $isRowEmpty) {
                $data += $rowObject
            }
        }
        Write-Log "  Successfully read $($data.Count) non-empty data rows." "DEBUG"

        # Return an object containing both the data and the environment value
        return [pscustomobject]@{
            Data = $data
            Environment = $environmentInExcel
        }

    } catch {
        Write-Log "Error importing Excel file '$($Path)': $_" "ERROR"
        Write-Log "  Stack trace: $($_.ScriptStackTrace)" "DEBUG"
        # Clean up COM object in case of error
        if ($workbook) { $workbook.Close($false) } # Close without saving
        if ($excel) { $excel.Quit() }
        if ($excel) { [System.Runtime.InteropServices.Marshal]::ReleaseComObject($excel) | Out-Null }
        Remove-Variable excel -ErrorAction SilentlyContinue
        throw "Failed to import Excel data."
    } finally {
        # Ensure COM object is released even on success
        if ($workbook) { $workbook.Close($false) } # Close without saving
        if ($excel) { $excel.Quit() }
        # Use a loop to ensure release, sometimes needed
        $releaseAttempts = 0
        while ([System.Runtime.InteropServices.Marshal]::ReleaseComObject($excel) -gt 0 -and $releaseAttempts -lt 10) {
            $releaseAttempts++
            Start-Sleep -Milliseconds 100
        }
        Remove-Variable excel -ErrorAction SilentlyContinue
        Write-Log "  Excel COM object cleanup attempted." "DEBUG"
    }
}
#endregion

#region F) ROLE MANAGEMENT FUNCTIONS
function Get-RoleIfExists {
    param([string]$RoleName)
    Write-Log "  Checking if role '$($RoleName)' exists (case-insensitive)..." "DEBUG" # Updated log message
    try {
        # Get all roles and filter by name case-insensitively
        $role = Get-VIRole -ErrorAction SilentlyContinue | Where-Object { $_.Name -ieq $RoleName } | Select-Object -First 1
        if ($role) {
            Write-Log "    Role '$($RoleName)' found (case-insensitive match)." "DEBUG" # Updated log message
            return $role
        } else {
            Write-Log "    Role '$($RoleName)' not found." "DEBUG"
            return $null
        }
    } catch {
        Write-Log "    Error checking for role '$($RoleName)': $_" "WARN"
        return $null
    }
}

function Clone-RoleFromSupportAdminTemplate {
    param([string]$NewRoleName)
    Write-Log "  Attempting to clone role from template for '$($NewRoleName)'..." "DEBUG"
    try {
        # Get the template role (assuming "SupportAdmin" exists)
        # Use case-insensitive lookup for the template role name
        $templateRole = Get-VIRole -ErrorAction SilentlyContinue | Where-Object { $_.Name -ieq "SupportAdmin" } | Select-Object -First 1

        if (-not $templateRole) {
            Write-Log "    Template role 'SupportAdmin' not found. Cannot clone role." "ERROR"
            return $null
        }

        # Clone the role using the exact case for the new role name
        $newRole = New-VIRole -Name $NewRoleName -Privilege (Get-Privilege -Role $templateRole) -ErrorAction Stop
        Write-Log "    Role '$($NewRoleName)' cloned successfully from 'SupportAdmin'." "DEBUG"
        return $newRole
    } catch {
        Write-Log "    Failed to clone role for '$($NewRoleName)': $_" "ERROR"
        Write-Log "    Error details: $($_.Exception.Message)" "DEBUG"
        return $null
    }
}

#endregion

#region G) PERMISSION ASSIGNMENT FUNCTION

function Assign-PermissionIfNeeded {
    param(
        [VMware.VimAutomation.ViCore.Impl.V1.Inventory.VirtualMachine]$VM,
        [string]$Principal, # e.g., "DOMAIN\Group" or "user@domain"
        [string]$RoleName
    )
    Write-Log "    Checking permissions for '$($Principal)' on VM '$($VM.Name)' with role '$($RoleName)'..." "DEBUG"

    try {
        # Get the role object (case-insensitive lookup handled by Get-RoleIfExists)
        $role = Get-RoleIfExists -RoleName $RoleName
        if (-not $role) {
            Write-Log "      Role '$($RoleName)' not found or could not be created. Cannot assign permission for '$($Principal)' on '$($VM.Name)'." "ERROR"
            return $false
        }

        # Check if the permission already exists (case-insensitive principal and role name comparison)
        $existingPermission = Get-VIPermission -Entity $VM -Principal $Principal -ErrorAction SilentlyContinue |
                              Where-Object { $_.Role -ieq $RoleName -and $_.Principal -ieq $Principal } # Ensure both match case-insensitively

        if ($existingPermission) {
            Write-Log "      Permission for '$($Principal)' with role '$($RoleName)' already exists on VM '$($VM.Name)', skipping." "DEBUG"
            return $true
        }

        # Assign the permission
        Write-Log "      Assigning permission for '$($Principal)' with role '$($RoleName)' on VM '$($VM.Name)'..." "DEBUG"
        New-VIPermission -Entity $VM -Principal $Principal -Role $role -Propagate:$false -ErrorAction Stop # Do not propagate to children by default
        Write-Log "      Permission assigned successfully." "DEBUG"
        return $true

    } catch {
        Write-Log "    Failed to assign permission for '$($Principal)' with role '$($RoleName)' on VM '$($VM.Name)': $_" "ERROR"
        Write-Log "    Error details: $($_.Exception.Message)" "DEBUG"
        return $false
    }
}

#endregion

#region H) TAGGING FUNCTIONS

# Note: Ensure-TagCategory, Get-TagIfExists, Ensure-Tag are defined in Region E and handle case-insensitivity

# Tagging logic is primarily within the main execution block (Region I)
# using the helper functions from Region E.

#endregion

#region I) MAIN EXECUTION
try {
    # Set the global script debug flag based on the parameter
    $global:ScriptDebugEnabled = $EnableScriptDebug.IsPresent

    #— Preflight and Connections —#
    Write-Log "Starting preflight checks..." "DEBUG"
    if (-not $vCenterServer) { throw "vCenterServer parameter is empty." }
    Write-Log "Testing connectivity to $($vCenterServer):443" "INFO"
    $test = Test-NetConnection $vCenterServer -Port 443 -ErrorAction Stop
    if (-not $test.TcpTestSucceeded) { throw "Cannot reach vCenter." }
    Write-Log "Connectivity OK." "INFO"
    if ($global:DefaultVIServers.Count -gt 0) {
        Write-Log "Disconnecting existing vCenter sessions" "INFO"
        Disconnect-VIServer -Server * -Confirm:$false -Force -ErrorAction SilentlyContinue
    }
    Write-Log "Setting PowerCLI certificate handling..." "DEBUG"
    Set-PowerCLIConfiguration -InvalidCertificateAction Ignore -Confirm:$false | Out-Null
    Write-Log "Connecting to vCenter $($vCenterServer)..." "DEBUG"
    $vc = Connect-VIServer -Server $vCenterServer -Credential $Credential -ErrorAction Stop
    Write-Log "Connected to vCenter (v$($vc.Version))." "INFO"
    if (Test-SsoModuleAvailable) {
        if (Connect-SsoAdminServer -Server $vCenterServer -Credential $Credential) { Write-Log "SSO connection established." "INFO" }
        else { Write-Log "SSO Admin connection failed. SSO-related features will be skipped." "WARN" }
    } else { Write-Log "SSO Admin module not available. SSO-related features will be skipped." "WARN"; $global:ssoConnected = $false }

    #— Import All Excel Sheets —#
    Write-Log "Preparing to import all required Excel sheets..." "DEBUG"
    $excelImportResult = Import-ExcelCOM -Path $ExcelFilePath -WorksheetName "DataSource" -ValidationCell "B1" -HeaderRow 3
    $osPatternImportResult = Import-ExcelCOM -Path $ExcelFilePath -WorksheetName "OSPatterns" -HeaderRow 1
    $configImportResult = Import-ExcelCOM -Path $ExcelFilePath -WorksheetName "Config" -HeaderRow 1

    #— Environment Safety Check and Configuration Setup —#
    $environmentInExcel = $excelImportResult.Environment
    $rows = $excelImportResult.Data
    $osPatternMapping = $osPatternImportResult.Data
    $configMapping = $configImportResult.Data
    if (-not ($Environment -ieq $environmentInExcel)) { throw "Environment Mismatch! Script was run with '-Environment $($Environment)', but the Excel file is set to '$($environmentInExcel)'." }
    $currentSecurityDomain = ($configMapping | Where-Object { $_.Environment -ieq $Environment }).SecurityGroupDomain | Select-Object -First 1
    if ([string]::IsNullOrWhiteSpace($currentSecurityDomain)) { throw "Could not find a valid SecurityGroupDomain for environment '$($Environment)' in the 'Config' sheet." }
    Write-Log "Safety check passed. Using Security Domain '$($currentSecurityDomain)' for environment '$($Environment)'." "INFO"

    #— Initialize Script Variables —#
    $processedCategories = @{}
    $ExcelValidOSTags = @{}
    $dcTag = $null

    #— Proactively Create and Whitelist All OS Tags from OSPatterns Sheet —#
    # <-- START OF NEW LOGIC
    Write-Log "Proactively creating and whitelisting all OS tags from the OSPatterns sheet..." "INFO"
    $osCategoryName = "vCenter-$($Environment)-OS"
    $osCategoryObj = Ensure-TagCategory -CategoryName $osCategoryName
    $processedCategories[$osCategoryName] = $osCategoryObj

    if ($osCategoryObj) {
        # Get unique tag names from the OSPatterns sheet to avoid redundant processing
        $uniqueOSTagNames = $osPatternMapping.TargetTagName | Select-Object -Unique | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
        Write-Log "Found $($uniqueOSTagNames.Count) unique OS tags to process in OSPatterns sheet." "DEBUG"
        foreach ($tagName in $uniqueOSTagNames) {
            $osTagToEnsure = Ensure-Tag -TagName $tagName -Category $osCategoryObj
            if ($osTagToEnsure) {
                Write-Log "  OS tag '$($tagName)' is ready. Adding to valid OS tag list." "DEBUG"
                $ExcelValidOSTags[$osTagToEnsure.Name] = $osTagToEnsure
            }
        }
    } else {
        Write-Log "Failed to create the master OS Category '$($osCategoryName)'. OS Tagging will be skipped." "ERROR"
    }
    # <-- END OF NEW LOGIC

    #— Process App-team and Function Rows from DataSource Sheet —#
    Write-Log "Beginning to process App-team and Function rows from DataSource sheet..." "INFO"
    foreach ($row in $rows) {
        try {
            $tagType = Get-ValueNormalized $row 'TagType'
            # <-- MODIFIED: This loop now ONLY processes App-team and Function types.
            if ($tagType -ieq 'App-team' -or $tagType -ieq 'Function') {
                $tagCategoryName = Get-ValueNormalized $row 'TagCategory'
                if ([string]::IsNullOrWhiteSpace($tagCategoryName)) { continue }

                if (-not $processedCategories.ContainsKey($tagCategoryName)) {
                    Write-Log "Ensuring category exists: '$($tagCategoryName)'..." "INFO"
                    $catObj = Ensure-TagCategory -CategoryName $tagCategoryName
                    $processedCategories[$tagCategoryName] = $catObj
                    
                    if ($tagCategoryName -match 'Function' -and (-not $dcTag)) {
                        $dcTag = Get-TagIfExists -TagName "Domain Controller" -CategoryName $tagCategoryName
                        if ($dcTag) { Write-Log "Found 'Domain Controller' Function Tag: $($dcTag.Name)" "INFO" }
                    }
                }
                
                if ($tagType -ieq 'App-team') {
                    $tagName = Get-ValueNormalized $row 'TagName'
                    $roleName = Get-ValueNormalized $row 'RoleName'
                    $secDomain = Get-ValueNormalized $row 'SecurityGroupDomain'
                    $secGroup = Get-ValueNormalized $row 'SecurityGroupName'
                    $categoryObj = $processedCategories[$tagCategoryName]

                    if (-not $categoryObj) {
                         Write-Log "Failed to ensure category '$($tagCategoryName)'. Skipping row." "ERROR"
                         continue
                    }
                    
                    Write-Log "  Row identified as 'App-team'. Ensuring tag exists..." "DEBUG"
                    $tagObj = Ensure-Tag -TagName $tagName -Category $categoryObj
                    if (-not $tagObj) {
                        Write-Log "    Failed to ensure tag '$($tagName)'. Skipping permission assignment for this row." "ERROR"
                        continue
                    }

                    if ([string]::IsNullOrWhiteSpace($roleName) -or [string]::IsNullOrWhiteSpace($secDomain) -or [string]::IsNullOrWhiteSpace($secGroup)) {
                        Write-Log "    Row for tag '$($tagName)' is missing RoleName or Security Group info. Tag created, but skipping permission logic." "WARN"
                        continue
                    }
                    
                    $vmsWithTag = Get-TagAssignment -Tag $tagObj | Where-Object { $_.Entity.GetType().Name -eq "VirtualMachine" } | ForEach-Object { Get-VM -Id $_.Entity.Id }
                    if ($vmsWithTag) {
                        Write-Log "    Found $($vmsWithTag.Count) VMs with tag '$($tagName)'. Assigning permissions..." "INFO"
                        foreach ($vm in $vmsWithTag) {
                            $isDC = $false
                            if ($dcTag) {
                                if ($dcTag.Id -in (Get-TagAssignment -Entity $vm).Tag.Id) { $isDC = $true }
                            }
                            if ($isDC -and $tagObj.Name -ieq 'Windows-server') {
                                Write-Log "    VM '$($vm.Name)' is a Domain Controller. Skipping assignment of 'Windows-server' permissions as per policy." "INFO"
                                continue
                            }
                            $principal = "$secDomain\$secGroup"
                            Assign-PermissionIfNeeded -VM $vm -Principal $principal -RoleName $roleName
                        }
                    }
                }
            }
        }
        catch { Write-Log "An unexpected error occurred while processing row: $_" "ERROR" }
    }
    Write-Log "Finished processing DataSource rows." "INFO"

#— Perform OS-based Tagging and Permission Assignment —#
    Write-Log "Starting OS-based tagging and permission assignment phase..." "INFO"
    if ($ExcelValidOSTags.Count -eq 0) {
        Write-Log "The list of valid OS tags from Excel is empty. Skipping OS-based operations." "WARN"
    }
    else {
        Write-Log "Valid OS tags for assignment: $($ExcelValidOSTags.Keys -join ', ')" "INFO"
        $allVMs = Get-View -ViewType VirtualMachine -Property 'Name', 'Config.GuestFullName' -Filter @{'Config.GuestFullName' = ".*"}
        Write-Log "Retrieved $($allVMs.Count) VMs for OS analysis." "DEBUG"

        foreach ($vm in $allVMs) {
            $vmName = $vm.Name
            $guestOsName = $vm.Config.GuestFullName
            if ([string]::IsNullOrWhiteSpace($guestOsName)) { continue }

            $mappingRow = $null
            foreach ($row in $osPatternMapping) {
                if ([string]::IsNullOrWhiteSpace($row.GuestOSPattern)) { continue }
                if ($guestOsName -match $row.GuestOSPattern) {
                    $mappingRow = $row
                    Write-Log "  VM '$($vmName)' with OS '$($guestOsName)' matched pattern '$($mappingRow.GuestOSPattern)'." "DEBUG"
                    break
                }
            }

            if ($null -eq $mappingRow) {
                Write-Log "  VM '$($vmName)' with OS '$($guestOsName)' did not match any defined OS patterns. Skipping." "DEBUG"
                continue
            }

            $targetTagName = Get-ValueNormalized $mappingRow 'TargetTagName'
            $targetRoleName = Get-ValueNormalized $mappingRow 'RoleName'
            $targetAdminGroup = Get-ValueNormalized $mappingRow 'AdminGroupName'

            if ($ExcelValidOSTags.ContainsKey($targetTagName)) {
                Write-Log "  Tag '$($targetTagName)' is in the Excel whitelist. Proceeding with operations for '$($vmName)'." "INFO"
                $vmObject = Get-VM -Name $vmName
                $osTagToAssign = $ExcelValidOSTags[$targetTagName]
                $currentTags = Get-TagAssignment -Entity $vmObject
                
                if ($osTagToAssign.Id -in $currentTags.Tag.Id) {
                    Write-Log "    VM '$($vmName)' is already tagged with '$($targetTagName)'. No action needed for tag." "DEBUG"
                } else {
                    Write-Log "    Assigning tag '$($targetTagName)' to VM '$($vmName)'..." "INFO"
                    New-TagAssignment -Tag $osTagToAssign -Entity $vmObject -ErrorAction Stop
                }

                if ([string]::IsNullOrWhiteSpace($targetRoleName) -or [string]::IsNullOrWhiteSpace($targetAdminGroup)) {
                    Write-Log "    OS Pattern for '$($targetTagName)' is missing RoleName or AdminGroupName in Excel. Skipping permission assignment." "WARN"
                } else {
                    $principal = "$($currentSecurityDomain)\$($targetAdminGroup)"
                    Assign-PermissionIfNeeded -VM $vmObject -Principal $principal -RoleName $targetRoleName
                }
            } else {
                Write-Log "  VM '$($vmName)' matched OS pattern for tag '$($targetTagName)', but this tag is NOT in the Excel whitelist. Skipping operations." "INFO"
            }
        }
    }

    # Specific logic for Domain Controller OS Tagging
    if ($dcTag) {
        Write-Log "Processing specific OS tagging for Domain Controllers..." "INFO"
        $dcOsTag = $ExcelValidOSTags["Domain-Controller"]
        if ($dcOsTag) {
            $vmsWithDcFunctionTag = Get-TagAssignment -Tag $dcTag | Where-Object { $_.Entity.GetType().Name -eq "VirtualMachine" } | ForEach-Object { Get-VM -Id $_.Entity.Id }
            Write-Log "Found $($vmsWithDcFunctionTag.Count) VMs with the 'Domain Controller' function tag." "DEBUG"
            foreach ($vm in $vmsWithDcFunctionTag) {
                $currentTags = Get-TagAssignment -Entity $vm
                if ($dcOsTag.Id -in $currentTags.Tag.Id) {
                    Write-Log "  VM '$($vm.Name)' is already tagged with 'Domain-Controller' OS tag. No action needed." "DEBUG"
                } else {
                    Write-Log "  Assigning 'Domain-Controller' OS tag to VM '$($vm.Name)'..." "INFO"
                    New-TagAssignment -Tag $dcOsTag -Entity $vm -ErrorAction Stop
                }
            }
        } else {
            Write-Log "OS Tag 'Domain-Controller' not found in the Excel whitelist. Skipping specific DC OS tagging." "WARN"
        }
    }
}
catch {
    Write-Log "FATAL error: $_" "ERROR"
    Write-Log "  Stack trace: $($_.ScriptStackTrace)" "DEBUG"
    Write-FallbackLog "FATAL: $_"
    throw
}
finally {
    Write-Log "Cleanup start" "INFO"
    Write-Log "  Beginning cleanup operations..." "DEBUG"

    if ($global:ssoConnected) {
        Write-Log "  SSO is connected, attempting to disconnect..." "DEBUG"
        try {
            Disconnect-SsoAdminServer -ErrorAction Stop
            $global:ssoConnected=$false
            Write-Log "SSO disconnected" "INFO"
            Write-Log "    SSO disconnect successful" "DEBUG"
        }
        catch {
            Write-Log "SSO disconnect failed: $_" "WARN"
            Write-Log "    Stack trace: $($_.ScriptStackTrace)" "DEBUG"
            Write-FallbackLog "SSO disconnect failed: $_"
        }
    } else {
        Write-Log "  SSO not connected, skipping disconnect" "DEBUG"
    }

    try {
        Write-Log "  Checking for vCenter connections..." "DEBUG"
        if ($global:DefaultVIServers.Count -gt 0) {
            Write-Log "    Found $($global:DefaultVIServers.Count) vCenter connections" "DEBUG"
            Disconnect-VIServer -Server * -Confirm:$false -Force -ErrorAction Stop
            Write-Log "vCenter disconnected" "INFO"
            Write-Log "    vCenter disconnect successful" "DEBUG"
        } else {
            Write-Log "    No vCenter connections found" "DEBUG"
        }
    }
    catch {
        Write-Log "vCenter disconnect failed: $_" "WARN"
        Write-Log "  Stack trace: $($_.ScriptStackTrace)" "DEBUG"
        Write-FallbackLog "vCenter disconnect failed: $_"
    }

    try {
        Write-Log "  Resetting PowerCLI certificate handling..." "DEBUG"
        Set-PowerCLIConfiguration -InvalidCertificateAction Warn -Confirm:$false | Out-Null
        Write-Log "    Certificate handling reset successful" "DEBUG"
    }
    catch {
        Write-Log "Failed to reset certificate policy: $_" "WARN"
        Write-Log "  Stack trace: $($_.ScriptStackTrace)" "DEBUG"
    }

    Write-Log "Cleanup complete" "INFO"
    Write-Log "Script execution finished" "DEBUG"
}
#endregion

