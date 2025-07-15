using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;
using UiDesktopApp2.Helpers;
using UiDesktopApp2.Models;
using UiDesktopApp2.Services;



namespace UiDesktopApp2.ViewModels.Pages
{
    public partial class MigrationViewModel : ObservableObject
    {
        private readonly ConnectionManager _connectionManager;
        private readonly PowerShellManager _powerShellManager;
        private readonly AppConfig _appConfig;

        [ObservableProperty]
        private ObservableCollection<VirtualMachineInfo> _availableVMs = new();

        [ObservableProperty]
        private ObservableCollection<VirtualMachineInfo> _selectedVMs = new();

        [ObservableProperty]
        private ICollectionView _filteredVMs;

        [ObservableProperty]
        private ObservableCollection<MigrationTask> _migrationQueue = new();

        [ObservableProperty]
        private ObservableCollection<MigrationTask> _activeMigrations = new();

        [ObservableProperty]
        private ObservableCollection<MigrationTask> _migrationHistory = new();

        // Migration Configuration
        [ObservableProperty]
        private ObservableCollection<string> _migrationTypes = new()
        {
            "Cold Migration", "vMotion", "Storage vMotion", "Cross-vCenter vMotion"
        };

        [ObservableProperty]
        private string _selectedMigrationType = "Cold Migration";

        [ObservableProperty]
        private ObservableCollection<string> _destinationClusters = new();

        [ObservableProperty]
        private string _selectedDestinationCluster = string.Empty;

        [ObservableProperty]
        private ObservableCollection<string> _destinationDatastores = new();

        [ObservableProperty]
        private string _selectedDestinationDatastore = string.Empty;

        [ObservableProperty]
        private ObservableCollection<string> _destinationResourcePools = new();

        [ObservableProperty]
        private string _selectedDestinationResourcePool = string.Empty;

        [ObservableProperty]
        private ObservableCollection<NetworkMapping> _networkMappings = new();

        // Migration Options
        [ObservableProperty]
        private bool _powerOffBeforeMigration = true;

        [ObservableProperty]
        private bool _createSnapshotBeforeMigration = true;

        [ObservableProperty]
        private bool _validateCompatibility = true;

        [ObservableProperty]
        private bool _deleteSourceAfterSuccess = false;

        [ObservableProperty]
        private bool _upgradeVMwareTools = false;

        [ObservableProperty]
        private bool _upgradeVMHardware = false;

        [ObservableProperty]
        private int _maxConcurrentMigrations = 3;

        // Scheduling
        [ObservableProperty]
        private bool _scheduleForLater = false;

        [ObservableProperty]
        private DateTime _scheduledStartTime = DateTime.Now.AddHours(1);

        [ObservableProperty]
        private ObservableCollection<string> _maintenanceWindows = new()
        {
            "Immediate", "Next Maintenance Window", "Weekend (Saturday 2 AM)", "Custom Time"
        };

        [ObservableProperty]
        private string _selectedMaintenanceWindow = "Immediate";

        // Progress and Status
        [ObservableProperty]
        private bool _isMigrationInProgress = false;

        [ObservableProperty]
        private double _overallProgress = 0;

        [ObservableProperty]
        private string _currentOperationStatus = "Ready to migrate";

        [ObservableProperty]
        private string _estimatedTimeRemaining = string.Empty;

        [ObservableProperty]
        private int _successfulMigrations = 0;

        [ObservableProperty]
        private int _failedMigrations = 0;

        [ObservableProperty]
        private int _pendingMigrations = 0;

        // Filtering
        [ObservableProperty]
        private string _vmSearchText = string.Empty;

        [ObservableProperty]
        private ObservableCollection<string> _powerStateFilters = new()
        {
            "All States", "Powered On", "Powered Off", "Suspended"
        };

        [ObservableProperty]
        private string _selectedPowerStateFilter = "All States";

        [ObservableProperty]
        private bool _showOnlyMigrationCandidates = true;

        public MigrationViewModel(ConnectionManager connectionManager, PowerShellManager powerShellManager, AppConfig appConfig)
        {
            _connectionManager = connectionManager;
            _powerShellManager = powerShellManager;
            _appConfig = appConfig;

            MaxConcurrentMigrations = appConfig.MaxConcurrentMigrations;

            // Initialize filtered collection
            _filteredVMs = CollectionViewSource.GetDefaultView(AvailableVMs);
            _filteredVMs.Filter = FilterVMs;

            LoadSampleData();
            LoadDestinationResources();
        }

        [RelayCommand]
        private async Task RefreshVMListAsync()
        {
            try
            {
                CurrentOperationStatus = "Refreshing VM inventory...";

                // In a real implementation, this would call PowerShell to get VMs
                await Task.Delay(2000);

                LoadSampleData();
                FilteredVMs.Refresh();

                CurrentOperationStatus = $"Loaded {AvailableVMs.Count} VMs from source vCenter";
            }
            catch (Exception ex)
            {
                CurrentOperationStatus = $"Error refreshing VMs: {ex.Message}";
            }
        }

        [RelayCommand]
        private void AddSelectedVMsToQueue()
        {
            var selected = SelectedVMs.Where(vm => vm.IsSelected).ToList();
            if (!selected.Any())
            {
                CurrentOperationStatus = "No VMs selected for migration";
                return;
            }

            foreach (var vm in selected)
            {
                if (!MigrationQueue.Any(task => task.ObjectName == vm.Name))
                {
                    var task = new MigrationTask
                    {
                        ObjectName = vm.Name,
                        ObjectType = "VirtualMachine",
                        Status = MigrationStatus.Queued,
                        Progress = 0,
                        StartTime = DateTime.Now,
                        Duration = TimeSpan.Zero,
                        Details = $"Type: {SelectedMigrationType}, Destination: {SelectedDestinationCluster}"
                    };
                    MigrationQueue.Add(task);
                }
            }

            PendingMigrations = MigrationQueue.Count(t => t.Status == MigrationStatus.Queued);
            CurrentOperationStatus = $"Added {selected.Count} VMs to migration queue";
        }

        [RelayCommand]
        private void RemoveFromQueue(MigrationTask task)
        {
            if (task != null && MigrationQueue.Contains(task))
            {
                MigrationQueue.Remove(task);
                PendingMigrations = MigrationQueue.Count(t => t.Status == MigrationStatus.Queued);
                CurrentOperationStatus = $"Removed {task.ObjectName} from migration queue";
            }
        }

        [RelayCommand]
        private async Task StartMigrationAsync()
        {
            if (!MigrationQueue.Any(t => t.Status == MigrationStatus.Queued))
            {
                CurrentOperationStatus = "No VMs queued for migration";
                return;
            }

            if (!ValidateMigrationSettings())
                return;

            try
            {
                IsMigrationInProgress = true;
                CurrentOperationStatus = "Starting migration process...";

                if (ScheduleForLater)
                {
                    await ScheduleMigrationAsync();
                }
                else
                {
                    await ExecuteMigrationAsync();
                }
            }
            catch (Exception ex)
            {
                CurrentOperationStatus = $"Migration failed: {ex.Message}";
                IsMigrationInProgress = false;
            }
        }

        [RelayCommand]
        private void StopMigration()
        {
            if (IsMigrationInProgress)
            {
                // In real implementation, this would cancel running PowerShell jobs
                IsMigrationInProgress = false;
                CurrentOperationStatus = "Migration stopped by user";

                // Move active migrations back to queue
                foreach (var activeMigration in ActiveMigrations.ToList())
                {
                    if (activeMigration.Status == MigrationStatus.InProgress)
                    {
                        activeMigration.Status = MigrationStatus.Cancelled;
                        activeMigration.Details = "Cancelled by user";
                        MigrationHistory.Add(activeMigration);
                    }
                }
                ActiveMigrations.Clear();
            }
        }

        [RelayCommand]
        private async Task ValidateSelectedVMsAsync()
        {
            var selected = SelectedVMs.Where(vm => vm.IsSelected).ToList();
            if (!selected.Any())
            {
                CurrentOperationStatus = "No VMs selected for validation";
                return;
            }

            try
            {
                CurrentOperationStatus = "Validating VM compatibility...";

                // Simulate validation process
                await Task.Delay(3000);

                var validCount = 0;
                var invalidCount = 0;

                foreach (var vm in selected)
                {
                    // Mock validation logic
                    if (vm.PowerState == "Powered On" && vm.ToolsStatus == "OK")
                    {
                        vm.IsMigrationCandidate = true;
                        validCount++;
                    }
                    else
                    {
                        vm.IsMigrationCandidate = false;
                        invalidCount++;
                    }
                }

                CurrentOperationStatus = $"Validation complete: {validCount} valid, {invalidCount} invalid VMs";
            }
            catch (Exception ex)
            {
                CurrentOperationStatus = $"Validation failed: {ex.Message}";
            }
        }

        [RelayCommand]
        private void LoadNetworkMappings()
        {
            try
            {
                var openFileDialog = new OpenFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                    Title = "Load Network Mappings"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    LoadNetworkMappingsFromFile(openFileDialog.FileName);
                    CurrentOperationStatus = $"Loaded network mappings from {openFileDialog.FileName}";
                }
            }
            catch (Exception ex)
            {
                CurrentOperationStatus = $"Failed to load network mappings: {ex.Message}";
            }
        }

        [RelayCommand]
        private void ExportMigrationPlan()
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "HTML files (*.html)|*.html|CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                    FileName = $"Migration_Plan_{DateTime.Now:yyyy-MM-dd_HH-mm}.html"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    GenerateMigrationPlan(saveFileDialog.FileName);
                    CurrentOperationStatus = $"Migration plan exported to {saveFileDialog.FileName}";
                }
            }
            catch (Exception ex)
            {
                CurrentOperationStatus = $"Export failed: {ex.Message}";
            }
        }

        [RelayCommand]
        private void ClearFilters()
        {
            VmSearchText = string.Empty;
            SelectedPowerStateFilter = "All States";
            ShowOnlyMigrationCandidates = false;
            FilteredVMs.Refresh();
            CurrentOperationStatus = "Filters cleared";
        }

        partial void OnVmSearchTextChanged(string value)
        {
            FilteredVMs.Refresh();
        }

        partial void OnSelectedPowerStateFilterChanged(string value)
        {
            FilteredVMs.Refresh();
        }

        partial void OnShowOnlyMigrationCandidatesChanged(bool value)
        {
            FilteredVMs.Refresh();
        }

        private bool FilterVMs(object item)
        {
            if (item is not VirtualMachineInfo vm)
                return false;

            // Power state filter
            if (SelectedPowerStateFilter != "All States" && vm.PowerState != SelectedPowerStateFilter)
                return false;

            // Search filter
            if (!string.IsNullOrWhiteSpace(VmSearchText))
            {
                var searchLower = VmSearchText.ToLower();
                if (!vm.Name.ToLower().Contains(searchLower) &&
                    !vm.Cluster.ToLower().Contains(searchLower) &&
                    !vm.Datastore.ToLower().Contains(searchLower))
                    return false;
            }

            // Migration candidates filter
            if (ShowOnlyMigrationCandidates && !vm.IsMigrationCandidate)
                return false;

            return true;
        }

        private bool ValidateMigrationSettings()
        {
            if (string.IsNullOrEmpty(SelectedDestinationCluster))
            {
                CurrentOperationStatus = "Please select a destination cluster";
                return false;
            }

            if (string.IsNullOrEmpty(SelectedDestinationDatastore))
            {
                CurrentOperationStatus = "Please select a destination datastore";
                return false;
            }

            return true;
        }

        private async Task ScheduleMigrationAsync()
        {
            CurrentOperationStatus = $"Migration scheduled for {ScheduledStartTime:yyyy-MM-dd HH:mm}";
            await Task.Delay(1000);
            IsMigrationInProgress = false;
        }

        private async Task ExecuteMigrationAsync()
        {
            var queuedTasks = MigrationQueue.Where(t => t.Status == MigrationStatus.Queued).ToList();
            var totalTasks = queuedTasks.Count;
            var completedTasks = 0;

            foreach (var task in queuedTasks)
            {
                if (ActiveMigrations.Count >= MaxConcurrentMigrations)
                {
                    // Wait for a slot to become available
                    await Task.Delay(5000);
                }

                // Start migration
                task.Status = MigrationStatus.InProgress;
                task.StartTime = DateTime.Now;
                task.Progress = 0;
                ActiveMigrations.Add(task);

                CurrentOperationStatus = $"Migrating {task.ObjectName}...";

                // Simulate migration process
                for (int progress = 0; progress <= 100; progress += 10)
                {
                    task.Progress = progress;
                    OverallProgress = (completedTasks + (progress / 100.0)) / totalTasks * 100;

                    if (!IsMigrationInProgress) // Check for cancellation
                        return;

                    await Task.Delay(500);
                }

                // Complete migration
                task.Status = MigrationStatus.Completed;
                task.Duration = DateTime.Now - task.StartTime;
                task.Progress = 100;
                ActiveMigrations.Remove(task);
                MigrationHistory.Add(task);

                completedTasks++;
                SuccessfulMigrations++;
                PendingMigrations = MigrationQueue.Count(t => t.Status == MigrationStatus.Queued);
            }

            IsMigrationInProgress = false;
            OverallProgress = 100;
            CurrentOperationStatus = $"Migration complete: {completedTasks} VMs migrated successfully";
        }

        private void LoadDestinationResources()
        {
            // In real implementation, this would query the destination vCenter
            DestinationClusters.Clear();
            DestinationClusters.Add("Destination-Cluster-01");
            DestinationClusters.Add("Destination-Cluster-02");
            DestinationClusters.Add("Destination-Cluster-03");

            DestinationDatastores.Clear();
            DestinationDatastores.Add("datastore-dest-01");
            DestinationDatastores.Add("datastore-dest-02");
            DestinationDatastores.Add("datastore-dest-ssd");

            DestinationResourcePools.Clear();
            DestinationResourcePools.Add("Default");
            DestinationResourcePools.Add("Production");
            DestinationResourcePools.Add("Development");
        }

        private void LoadSampleData()
        {
            AvailableVMs.Clear();

            var sampleVMs = new[]
            {
                new VirtualMachineInfo { Name = "WebServer01", PowerState = "Powered On", GuestOS = "Windows Server 2019", CpuCount = 4, MemoryGB = 16, StorageGB = 250, Cluster = "Prod-Cluster-01", Host = "esxi01.domain.com", Datastore = "datastore-prod-01", Network = "Production-VLAN100", ToolsStatus = "OK", VCenter = "vcenter01.domain.com", IsMigrationCandidate = true },
                new VirtualMachineInfo { Name = "DatabaseServer01", PowerState = "Powered On", GuestOS = "Windows Server 2022", CpuCount = 8, MemoryGB = 64, StorageGB = 1000, Cluster = "Prod-Cluster-01", Host = "esxi02.domain.com", Datastore = "datastore-prod-02", Network = "Production-VLAN100", ToolsStatus = "OK", VCenter = "vcenter01.domain.com", IsMigrationCandidate = true },
                new VirtualMachineInfo { Name = "AppServer01", PowerState = "Powered On", GuestOS = "Ubuntu Server 20.04", CpuCount = 2, MemoryGB = 8, StorageGB = 100, Cluster = "Prod-Cluster-01", Host = "esxi03.domain.com", Datastore = "datastore-prod-01", Network = "Production-VLAN100", ToolsStatus = "OK", VCenter = "vcenter01.domain.com", IsMigrationCandidate = true },
                new VirtualMachineInfo { Name = "TestServer01", PowerState = "Powered Off", GuestOS = "Windows Server 2016", CpuCount = 2, MemoryGB = 4, StorageGB = 80, Cluster = "Test-Cluster-01", Host = "esxi04.domain.com", Datastore = "datastore-test-01", Network = "Development-VLAN300", ToolsStatus = "Out of Date", VCenter = "vcenter01.domain.com", IsMigrationCandidate = false },
                new VirtualMachineInfo { Name = "FileServer01", PowerState = "Powered On", GuestOS = "Windows Server 2019", CpuCount = 4, MemoryGB = 32, StorageGB = 2000, Cluster = "Prod-Cluster-02", Host = "esxi05.domain.com", Datastore = "datastore-prod-03", Network = "Production-VLAN100", ToolsStatus = "OK", VCenter = "vcenter01.domain.com", IsMigrationCandidate = true }
            };

            foreach (var vm in sampleVMs)
            {
                AvailableVMs.Add(vm);
            }
        }

        private void LoadNetworkMappingsFromFile(string filePath)
        {
            NetworkMappings.Clear();
            var lines = File.ReadAllLines(filePath);

            foreach (var line in lines.Skip(1)) // Skip header
            {
                var parts = line.Split(',');
                if (parts.Length >= 2)
                {
                    NetworkMappings.Add(new NetworkMapping
                    {
                        SourceNetwork = parts[0].Trim(),
                        DestinationNetwork = parts[1].Trim()
                    });
                }
            }
        }

        private void GenerateMigrationPlan(string filePath)
        {
            var html = $@"
<!DOCTYPE html>
<html>
<head>
    <title>Migration Plan - {DateTime.Now:yyyy-MM-dd}</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 20px; }}
        .header {{ color: #2c3e50; border-bottom: 2px solid #3498db; padding-bottom: 10px; }}
        .vm-list {{ margin: 20px 0; }}
        .vm-item {{ padding: 10px; background: #f8f9fa; margin: 5px 0; border-radius: 5px; }}
        .settings {{ margin: 20px 0; background: #e8f4f8; padding: 15px; border-radius: 5px; }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>vCenter Migration Plan</h1>
        <p>Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>
    </div>
    
    <div class='settings'>
        <h2>Migration Settings</h2>
        <p><strong>Migration Type:</strong> {SelectedMigrationType}</p>
        <p><strong>Destination Cluster:</strong> {SelectedDestinationCluster}</p>
        <p><strong>Destination Datastore:</strong> {SelectedDestinationDatastore}</p>
        <p><strong>Max Concurrent:</strong> {MaxConcurrentMigrations}</p>
        <p><strong>Power Off Before Migration:</strong> {PowerOffBeforeMigration}</p>
        <p><strong>Create Snapshot:</strong> {CreateSnapshotBeforeMigration}</p>
        <p><strong>Validate Compatibility:</strong> {ValidateCompatibility}</p>
    </div>
    
    <div class='vm-list'>
        <h2>VMs to Migrate ({MigrationQueue.Count})</h2>
        {string.Join("", MigrationQueue.Select(task => $"<div class='vm-item'>{task.ObjectName} - {task.Details}</div>"))}
    </div>
</body>
</html>";

            File.WriteAllText(filePath, html);
        }
    }
}
