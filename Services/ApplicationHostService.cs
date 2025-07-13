using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using UiDesktopApp2.Views.Pages;
using UiDesktopApp2.Views.Windows;
using Wpf.Ui;

public class ApplicationHostService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private INavigationWindow? _navigationWindow;

    public ApplicationHostService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await HandleActivationAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
    }

    private async Task HandleActivationAsync()
    {
        await Task.CompletedTask;

        try
        {
            if (!System.Windows.Application.Current.Windows.OfType<MainWindow>().Any())
            {
                _navigationWindow = _serviceProvider.GetRequiredService<INavigationWindow>();
                _navigationWindow?.ShowWindow();

                // Optional: Navigate to default page
                if (_navigationWindow != null)
                {
                    _navigationWindow.Navigate(typeof(DashboardPage));
                }
            }
            else
            {
                System.Windows.Application.Current.Windows.OfType<MainWindow>().First().Activate();
            }
        }
        catch (Exception ex)
        {
            // Log the error
            System.Diagnostics.Debug.WriteLine($"Activation Error: {ex.Message}");

            // Optionally show an error message
            System.Windows.MessageBox.Show(
                $"Failed to start application: {ex.Message}",
                "Startup Error",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error
            );
        }
    }
}

