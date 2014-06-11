using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace FSActiveFires {
    /// <summary>
    /// Adapted from example on StackOverflow https://stackoverflow.com/a/1316417
    /// </summary>
    abstract class NotifyPropertyChanged : INotifyPropertyChanged {
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null) {
            if (EqualityComparer<T>.Default.Equals(field, value)) {
                return false;
            }
            else {
                field = value;
                OnPropertyChanged(propertyName);
                return true;
            }
        }
    }
}
