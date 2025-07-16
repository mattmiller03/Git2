using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using UiDesktopApp2.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;

namespace UiDesktopApp2.Views.Pages
{
    public partial class BackupPage : Page, INavigableView<BackupViewModel>
    {
        public BackupViewModel ViewModel { get; }

        public BackupPage(BackupViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = ViewModel;
            InitializeComponent();

            // Load data when page loads
            Loaded += async (s, e) => await ViewModel.LoadDataAsync();
        }
    }
}
