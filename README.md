# UiDesktopApp2


vCenter Migration Tool WPF Application - Development Status
Overview
This project is a WPF desktop application built with .NET, using the WPF UI 4.0.2 framework for modern UI elements and Microsoft.Extensions.Hosting for dependency injection and application lifecycle management. It facilitates migration of virtual machines between vCenter servers with advanced monitoring, backup, and configuration features.

Current State (Version 1.0)
The application successfully starts up without errors.
Dependency Injection (DI) is fully configured via App.xaml.cs using Microsoft.Extensions.Hosting.
The main window (MainWindow) is created via DI; the StartupUri has been removed from App.xaml to enable this.
Services and ViewModels (e.g., ConnectionManager, DashboardViewModel, PowerShellManager) are registered properly and injected.
Theme management is configured to read from appsettings.json and applied statically on startup using Wpf.Ui.Appearance.ApplicationThemeManager.
PowerShell scripts are registered and managed via PowerShellScriptManager and executed through PowerShellManager.
Credentials are securely managed using WindowsCredentialManager implementing ICredentialManager.
Profiles store connection info and are serialized via JsonProfileManager implementing IProfileManager.
Navigation is handled by NavigationViewPageProvider and INavigationService, integrated with WPF UI controls.




Key Files and Their Roles
File	Role / Description
App.xaml.cs	Application entry point, DI registration, startup handling
MainWindow.xaml(.cs)	Main window UI and DI-constructed ViewModel injection
ConnectionManager.cs	Handles vCenter connection profiles and connection tests
JsonProfileManager.cs	Manages saving/loading connection profiles as JSON
WindowsCredentialManager.cs	Secure storage of passwords in Windows Credential Manager
PowerShellManager.cs	Executes PowerShell scripts for migration operations
PowerShellScriptManager.cs	Manages registering and locating PowerShell scripts
DashboardViewModel.cs	ViewModel for the dashboard page, shows profile statuses
ApplicationHostService.cs	Hosted service to manage app startup and window activation
NavigationViewPageProvider.cs	Maps page types to concrete page classes for navigation
AppConfig.cs	Configuration class mapped from appsettings.json
Various ViewModel files	MVVM ViewModels for different pages (Backup, Logs, Settings, etc.)




Important Notes for Next Developers or Chatbots
DI and Startup: The app uses constructor injection extensively. All classes requiring dependencies must be registered in DI.
MainWindow Creation: The app manually creates and shows MainWindow via DI (INavigationWindow interface). Do not use StartupUri.
Theme Management: Theme is configured from appsettings.json under AppConfig.ApplicationTheme. It uses ApplicationThemeManager from WPF UI 4.0.2 to apply theme globally at startup.
PowerShell Integration: PowerShell scripts are required for vCenter operations and must be present in the Scripts folder. The app registers them at startup.
Credential Storage: Passwords are stored securely using Windows Credential Manager via WindowsCredentialManager.
Profiles: Connection profiles are persisted as JSON files using JsonProfileManager.
Navigation: Uses INavigationService and NavigationViewPageProvider linked to WPF UI navigation controls.




Suggestions for Future Work
Implement UI pages fully (some placeholders exist).
Add profile create/edit dialogs and validation.
Improve error handling and logging throughout.
Add unit tests for services and ViewModels.
Consider localization/globalization support.



How to Build and Run
Ensure .NET 7 or compatible SDK is installed.
Restore NuGet packages.
Build the solution.
Confirm PowerShell scripts exist in the Scripts folder.
Run the app; the main window should appear with dashboard loaded.
Configure connection profiles and test connectivity.
