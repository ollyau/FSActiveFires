using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace FSActiveFires {
    /// <summary>
    /// Adapted from http://marlongrech.wordpress.com/2008/11/26/avoiding-commandbinding-in-the-xaml-code-behind-files/
    /// and http://blogs.msdn.com/b/mikehillberg/archive/2009/03/20/icommand-is-like-a-chocolate-cake.aspx
    /// and http://msdn.microsoft.com/en-us/magazine/dd419663.aspx#id0090030
    /// and https://stackoverflow.com/a/15201785 (binding window closing)
    /// and https://stackoverflow.com/a/11077841 (binding window closing)
    /// </summary>
    class RelayCommand : ICommand {
        readonly Action<object> _execute;
        readonly Predicate<object> _canExecute;

        public RelayCommand(Action<object> execute, Predicate<object> canExecute = null) {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) {
            return (_canExecute != null) ? _canExecute(parameter) : true;
        }

        public event EventHandler CanExecuteChanged {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void Execute(object parameter) {
            _execute(parameter);
        }
    }
}
