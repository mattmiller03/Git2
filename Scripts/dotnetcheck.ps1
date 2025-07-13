# Comprehensive WPF Application Startup Diagnostic Script

function Diagnose-WPFStartup {
    param (
        [string]$ProjectPath = "C:\Git2\UiDesktopApp2"
    )

    # Check Project Structure
    Write-Host "Checking Project Structure:" -ForegroundColor Cyan
    Get-ChildItem $ProjectPath -Recurse | Where-Object {$_.Name -match "App.xaml|MainWindow.xaml|Program.cs"} | ForEach-Object {
        Write-Host "Found: $($_.FullName)" -ForegroundColor Green
    }

    # Verify .NET SDK and Runtime
    Write-Host "`nVerifying .NET Environment:" -ForegroundColor Cyan
    dotnet --version
    
    # Check for Potential Startup Exceptions
    Write-Host "`nPreparing Diagnostic Build:" -ForegroundColor Cyan
    
    # Add Verbose Logging to Startup
    $diagnosisCode = @"
using System;
using System.Windows;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

public partial class App : Application 
{
    protected override void OnStartup(StartupEventArgs e)
    {
        try 
        {
            base.OnStartup(e);
            
            // Enhanced Logging
            AppDomain.CurrentDomain.UnhandledException += (s, ex) => {
                MessageBox.Show($"Unhandled Exception: {ex.ExceptionObject}", "Critical Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(1);
            };

            // Your existing startup logic here
            // Add detailed logging or breakpoints
            Console.WriteLine("Application Starting...");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Startup Error: {ex.Message}\n\nStack Trace: {ex.StackTrace}", 
                            "Startup Failure", 
                            MessageBoxButton.OK, 
                            MessageBoxImage.Error);
            Environment.Exit(1);
        }
    }
}
"@

    # Nullability Warning Fix Suggestion
    Write-Host "`nNullability Warning Fix:" -ForegroundColor Yellow
    Write-Host "In JsonProfileManager.cs, modify GetProfile method:" -ForegroundColor Cyan
    Write-Host @"
    // Change method signature to explicitly handle nullability
    public ConnectionProfile? GetProfile(string name)
    {
        // Existing implementation
        // Ensure you're handling potential null scenarios
        return // your existing return logic
    }
"@ -ForegroundColor Green

    # Dependency Injection Verification
    Write-Host "`nDependency Injection Configuration Check:" -ForegroundColor Cyan
    Write-Host "Ensure your service configuration in Program.cs or Startup.cs includes:" -ForegroundColor White
    Write-Host @"
    services.AddSingleton<MainWindow>();
    services.AddSingleton<INavigationWindow>(sp => sp.GetRequiredService<MainWindow>());
"@ -ForegroundColor Green
}

# Run Diagnostics
Diagnose-WPFStartup
