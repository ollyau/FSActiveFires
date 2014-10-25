using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace FSActiveFires {
    class RelayCommandAsync : ICommand {
        readonly Func<object, Task> _asyncExecute;
        readonly Predicate<object> _canExecute;
        private Task _task;

        public RelayCommandAsync(Func<object, Task> asyncExecute, Predicate<object> canExecute = null) {
            _asyncExecute = asyncExecute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) {
            return (_task == null || _task.IsCompleted) && ((_canExecute != null) ? _canExecute(parameter) : true);
        }

        public event EventHandler CanExecuteChanged {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public async void Execute(object parameter) {
            _task = _asyncExecute(parameter);
            CommandManager.InvalidateRequerySuggested();
            await _task;
            CommandManager.InvalidateRequerySuggested();
        }
    }
}
