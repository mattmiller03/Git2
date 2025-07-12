using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace UiDesktopApp2.Services
{
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute; // Fix for CS8625: Allow null for _canExecute  

        public RelayCommand(Action execute, Func<bool>? canExecute = null) // Fix for CS8625: Allow null for canExecute  
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) // Fix for CS8767: Match nullability of ICommand.CanExecute  
            => _canExecute?.Invoke() ?? true;

        public void Execute(object? parameter) // Ensure nullability matches ICommand.Execute  
            => _execute();

        public event EventHandler? CanExecuteChanged = delegate { }; // Fix for CS8618: Initialize CanExecuteChanged with a default delegate  

        /// <summary>  
        /// Call this to re-evaluate whether the command can run.  
        /// </summary>  
        public void RaiseCanExecuteChanged()
            => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
