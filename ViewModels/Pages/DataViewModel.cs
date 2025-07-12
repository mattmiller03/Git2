using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using UiDesktopApp2.Models;
using UiDesktopApp2.Services;

namespace UiDesktopApp2.ViewModels.Pages
{
    public partial class DataViewModel : ObservableObject
    {
        private readonly ConnectionManager _connectionManager;
        private readonly PowerShellManager _powerShellManager;

        [ObservableProperty]
        private ObservableCollection<VirtualMachineInfo> _virtualMachines = new();

        [ObservableProperty]
        private ObservableCollection<VirtualMachineInfo> _selectedVirtualMachines = new();

        [ObservableProperty]
        private ICollectionView _filteredVirtualMachines;

        [ObservableProperty]
        private ObservableCollection<string> _vcenterOptions = new()
        {
            "All vCenters", "vcenter01.domain.com", "vcenter02.domain.com"
        };

        [ObservableProperty]
        private string _selectedVcenter = "All vCenters";

        [ObservableProperty]
        private ObservableCollection<string> _powerStateOptions = new()
        {
            "All States", "Powered On", "Powered Off", "Suspended"
        };

        [ObservableProperty]
        private string _selectedPowerState = "All States";

        [ObservableProperty]
        private ObservableCollection<string> _osTypeOptions = new()
        {
            "All OS", "Windows", "Linux", "Other"
        };

        [ObservableProperty]
        private string _selectedOsType = "All OS";

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private bool _showOnlyMigrationCandidates = false;

        [ObservableProperty]
        private bool _isLoading = false;

        [ObservableProperty]
        private string _statusMessage = "Ready";

        // Summary Statistics
        [ObservableProperty]
        private int _totalVMs = 0;

        [ObservableProperty]
        private int _poweredOnVMs = 0;

        [ObservableProperty]
        private int _poweredOffVMs = 0;

        [ObservableProperty]
        private int _windowsVMs = 0;

        [ObservableProperty]
        private int _linuxVMs = 0;

        [ObservableProperty]
        private int _displayedVMs = 0;

        // Storage and Network Data
        [ObservableProperty]
        private ObservableCollection<DatastoreInfo> _datastores = new();

        [ObservableProperty]
        private ObservableCollection<NetworkInfo> _networks = new();

        public DataViewModel(ConnectionManager connectionManager, PowerShellManager powerShellManager)
        {
            _connectionManager = connectionManager;
            _powerShellManager = powerShellManager;

            // Initialize filtered collection
            _filteredVirtualMachines = CollectionViewSource.GetDefaultView(VirtualMachines);
            _filteredVirtualMachines.Filter = FilterVirtualMachines;

            // Load sample data
            LoadSampleData();
            UpdateStatistics();
        }

        [RelayCommand]
        private async Task RefreshDataAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Refreshing VM inventory...";

                // In a real implementation, this would call PowerShell scripts to get actual data
                await Task.Delay(2000); // Simulate API call

                LoadSampleData();
                UpdateStatistics();
                FilteredVirtualMachines.Refresh();

                StatusMessage = $"Refreshed {TotalVMs} VMs successfully";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error refreshing data: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void ClearFilters()
        {
            SelectedVcenter = "All vCenters";
            SelectedPowerState = "All States";
            SelectedOsType = "All OS";
            SearchText = string.Empty;
            ShowOnlyMigrationCandidates = false;

            FilteredVirtualMachines.Refresh();
            UpdateDisplayedCount();
            StatusMessage = "Filters cleared";
        }

        [RelayCommand]
        private void SelectAllVMs()
        {
            SelectedVirtualMachines.Clear();
            foreach (var vm in FilteredVirtualMachines.Cast<VirtualMachineInfo>())
            {
                vm.IsSelected = true;
                SelectedVirtualMachines.Add(vm);
            }
            StatusMessage = $"Selected {SelectedVirtualMachines.Count} VMs";
        }

        [RelayCommand]
        private void ClearSelection()
        {
            foreach (var vm in VirtualMachines)
            {
                vm.IsSelected = false;
            }
            SelectedVirtualMachines.Clear();
            StatusMessage = "Selection cleared";
        }

        [RelayCommand]
        private void AddToMigrationQueue()
        {
            var selectedCount = SelectedVirtualMachines.Count;
            if (selectedCount == 0)
            {
                StatusMessage = "No VMs selected for migration";
                return;
            }

            // In a real implementation, this would add VMs to a migration queue
            StatusMessage = $"Added {selectedCount} VMs to migration queue";
        }

        [RelayCommand]
        private void ExportData()
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv|Excel files (*.xlsx)|*.xlsx|All files (*.*)|*.*",
                    FileName = $"VM_Inventory_{DateTime.Now:yyyy-MM-dd}.csv"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    ExportToCsv(saveFileDialog.FileName);
                    StatusMessage = $"Data exported to {saveFileDialog.FileName}";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Export failed: {ex.Message}";
            }
        }

        [RelayCommand]
        private void ExportSelected()
        {
            if (SelectedVirtualMachines.Count == 0)
            {
                StatusMessage = "No VMs selected for export";
                return;
            }

            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv|Excel files (*.xlsx)|*.xlsx|All files (*.*)|*.*",
                    FileName = $"Selected_VMs_{DateTime.Now:yyyy-MM-dd}.csv"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    ExportToCsv(saveFileDialog.FileName, SelectedVirtualMachines);
                    StatusMessage = $"Selected VMs exported to {saveFileDialog.FileName}";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Export failed: {ex.Message}";
            }
        }

        [RelayCommand]
        private void ViewDetails()
        {
            if (SelectedVirtualMachines.Count != 1)
            {
                StatusMessage = "Please select exactly one VM to view details";
                return;
            }

            // In a real implementation, this would open a details window
            var vm = SelectedVirtualMachines.First();
            StatusMessage = $"Viewing details for {vm.Name}";
        }

        partial void OnSelectedVcenterChanged(string value)
        {
            FilteredVirtualMachines.Refresh();
            UpdateDisplayedCount();
        }

        partial void OnSelectedPowerStateChanged(string value)
        {
            FilteredVirtualMachines.Refresh();
            UpdateDisplayedCount();
        }

        partial void OnSelectedOsTypeChanged(string value)
        {
            FilteredVirtualMachines.Refresh();
            UpdateDisplayedCount();
        }

        partial void OnSearchTextChanged(string value)
        {
            FilteredVirtualMachines.Refresh();
            UpdateDisplayedCount();
        }

        partial void OnShowOnlyMigrationCandidatesChanged(bool value)
        {
            FilteredVirtualMachines.Refresh();
            UpdateDisplayedCount();
        }

        private bool FilterVirtualMachines(object item)
        {
            if (item is not VirtualMachineInfo vm)
                return false;

            // vCenter filter
            if (SelectedVcenter != "All vCenters" && vm.VCenter != SelectedVcenter)
                return false;

            // Power state filter
            if (SelectedPowerState != "All States" && vm.PowerState != SelectedPowerState)
                return false;

            // OS type filter
            if (SelectedOsType != "All OS")
            {
                var osType = GetOsType(vm.GuestOS);
                if (osType != SelectedOsType)
                    return false;
            }

            // Search filter
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLower();
                if (!vm.Name.ToLower().Contains(searchLower) &&
                    !vm.Cluster.ToLower().Contains(searchLower) &&
                    !vm.Datastore.ToLower().Contains(searchLower) &&
                    !vm.Host.ToLower().Contains(searchLower))
                    return false;
            }

            // Migration candidates filter
            if (ShowOnlyMigrationCandidates && !vm.IsMigrationCandidate)
                return false;

            return true;
        }

        private void UpdateStatistics()
        {
            TotalVMs = VirtualMachines.Count;
            PoweredOnVMs = VirtualMachines.Count(vm => vm.PowerState == "Powered On");
            PoweredOffVMs = VirtualMachines.Count(vm => vm.PowerState == "Powered Off");
            WindowsVMs = VirtualMachines.Count(vm => GetOsType(vm.GuestOS) == "Windows");
            LinuxVMs = VirtualMachines.Count(vm => GetOsType(vm.GuestOS) == "Linux");

            UpdateDisplayedCount();
        }

        private void UpdateDisplayedCount()
        {
            DisplayedVMs = FilteredVirtualMachines.Cast<VirtualMachineInfo>().Count();
        }

        private string GetOsType(string guestOS)
        {
            if (guestOS.Contains("Windows", StringComparison.OrdinalIgnoreCase))
                return "Windows";
            if (guestOS.Contains("Linux", StringComparison.OrdinalIgnoreCase) ||
                guestOS.Contains("Ubuntu", StringComparison.OrdinalIgnoreCase) ||
                guestOS.Contains("RedHat", StringComparison.OrdinalIgnoreCase) ||
                guestOS.Contains("CentOS", StringComparison.OrdinalIgnoreCase))
                return "Linux";
            return "Other";
        }

        private void LoadSampleData()
        {
            VirtualMachines.Clear();

            // Sample VM data
            var sampleVMs = new[]
            {
                new VirtualMachineInfo { Name = "WebServer01", PowerState = "Powered On", GuestOS = "Windows Server 2019", CpuCount = 4, MemoryGB = 16, StorageGB = 250, Cluster = "Prod-Cluster-01", Host = "esxi01.domain.com", Datastore = "datastore-prod-01", Network = "Production-VLAN100", ToolsStatus = "OK", VCenter = "vcenter01.domain.com", IsMigrationCandidate = true },
                new VirtualMachineInfo { Name = "WebServer02", PowerState = "Powered On", GuestOS = "Windows Server 2019", CpuCount = 4, MemoryGB = 16, StorageGB = 250, Cluster = "Prod-Cluster-01", Host = "esxi02.domain.com", Datastore = "datastore-prod-01", Network = "Production-VLAN100", ToolsStatus = "OK", VCenter = "vcenter01.domain.com", IsMigrationCandidate = true },
                new VirtualMachineInfo { Name = "DatabaseServer01", PowerState = "Powered On", GuestOS = "Windows Server 2022", CpuCount = 8, MemoryGB = 64, StorageGB = 1000, Cluster = "Prod-Cluster-01", Host = "esxi01.domain.com", Datastore = "datastore-prod-02", Network = "Production-VLAN100", ToolsStatus = "OK", VCenter = "vcenter01.domain.com", IsMigrationCandidate = true },
                new VirtualMachineInfo { Name = "AppServer01", PowerState = "Powered On", GuestOS = "Ubuntu Server 20.04", CpuCount = 2, MemoryGB = 8, StorageGB = 100, Cluster = "Prod-Cluster-01", Host = "esxi03.domain.com", Datastore = "datastore-prod-01", Network = "Production-VLAN100", ToolsStatus = "OK", VCenter = "vcenter01.domain.com", IsMigrationCandidate = true },
                new VirtualMachineInfo { Name = "TestServer01", PowerState = "Powered Off", GuestOS = "Windows Server 2016", CpuCount = 2, MemoryGB = 4, StorageGB = 80, Cluster = "Test-Cluster-01", Host = "esxi04.domain.com", Datastore = "datastore-test-01", Network = "Development-VLAN300", ToolsStatus = "Out of Date", VCenter = "vcenter01.domain.com", IsMigrationCandidate = false },
                // Add more sample data...
            };

            foreach (var vm in sampleVMs)
            {
                VirtualMachines.Add(vm);
            }

            // Load datastore info
            Datastores.Clear();
            Datastores.Add(new DatastoreInfo { Name = "datastore-prod-01", UsedGB = 2150, TotalGB = 4000, UsagePercentage = 52.5 });
            Datastores.Add(new DatastoreInfo { Name = "datastore-prod-02", UsedGB = 3800, TotalGB = 4000, UsagePercentage = 95.0 });
            Datastores.Add(new DatastoreInfo { Name = "datastore-test-01", UsedGB = 850, TotalGB = 2000, UsagePercentage = 42.5 });

            // Load network info
            Networks.Clear();
            Networks.Add(new NetworkInfo { Name = "Production-VLAN100", VMCount = 89 });
            Networks.Add(new NetworkInfo { Name = "DMZ-VLAN200", VMCount = 45 });
            Networks.Add(new NetworkInfo { Name = "Development-VLAN300", VMCount = 67 });
            Networks.Add(new NetworkInfo { Name = "Management-VLAN400", VMCount = 23 });
            Networks.Add(new NetworkInfo { Name = "Other Networks", VMCount = 23 });
        }

        private void ExportToCsv(string filePath, IEnumerable<VirtualMachineInfo>? vmsToExport = null)
        {
            var vms = vmsToExport ?? VirtualMachines;
            var lines = new List<string>
            {
                "Name,PowerState,GuestOS,vCPU,MemoryGB,StorageGB,Cluster,Host,Datastore,Network,ToolsStatus,vCenter"
            };

            foreach (var vm in vms)
            {
                lines.Add($"{vm.Name},{vm.PowerState},{vm.GuestOS},{vm.CpuCount},{vm.MemoryGB},{vm.StorageGB},{vm.Cluster},{vm.Host},{vm.Datastore},{vm.Network},{vm.ToolsStatus},{vm.VCenter}");
            }

            System.IO.File.WriteAllLines(filePath, lines);
        }
    }
}

