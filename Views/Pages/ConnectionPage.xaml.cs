using System.Windows.Controls;
using UiDesktopApp2.ViewModels.Pages;

namespace UiDesktopApp2.Views.Pages
{
    public partial class ConnectionPage : Page
    {
        public ConnectionPage(ConnectionViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}