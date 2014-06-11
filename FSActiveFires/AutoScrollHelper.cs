using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FSActiveFires {
    /// <summary>
    /// Auto scroll class
    /// Based on code from stackoverflow user Xin
    /// https://stackoverflow.com/a/8372627
    /// </summary>
    class AutoScrollHelper {
        public static bool GetAutoScroll(DependencyObject obj) {
            return (bool)obj.GetValue(AutoScrollProperty);
        }

        public static void SetAutoScroll(DependencyObject obj, bool value) {
            obj.SetValue(AutoScrollProperty, value);
        }

        public static readonly DependencyProperty AutoScrollProperty =
            DependencyProperty.RegisterAttached("AutoScroll", typeof(bool), typeof(AutoScrollHelper), new PropertyMetadata(false, AutoScrollPropertyChanged));

        private static void AutoScrollPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var scrollViewer = d as System.Windows.Controls.ScrollViewer;

            if (scrollViewer != null && (bool)e.NewValue) {
                scrollViewer.ScrollToBottom();
            }
        }
    }
}
