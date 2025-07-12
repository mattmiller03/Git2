using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wpf.Ui.Abstractions;

namespace UiDesktopApp2.Services
{

        public class NavigationViewPageProvider : INavigationViewPageProvider
        {
            // Implementing the required interface member
            public object? GetPage(Type pageType)
            {
                // Example implementation: return null for now
                return null;
            }
    }
    
}
