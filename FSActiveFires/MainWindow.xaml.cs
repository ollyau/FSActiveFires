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

namespace FSActiveFires {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        public MainWindow() {
            InitializeComponent();

            MainViewModel vm = (MainViewModel)this.DataContext;
            Closing += vm.MainWindow_Closing;
            Button_Download.Click += vm.Button_Download_Click;
            Button_Test.Click += vm.Button_Test_Click;
            Button_Connect.Click += vm.Button_Connect_Click;
            Button_Install.Click += vm.Button_Install_Click;
            //Button_CreateObjects.Click += vm.Button_CreateObjects_Click;
            //Button_RemoveObjects.Click += vm.Button_RemoveObjects_Click;
        }
    }

    /// <summary>
    /// Auto scroll class
    /// Based on code from stackoverflow user Xin
    /// https://stackoverflow.com/a/8372627
    /// </summary>
    public static class AutoScrollHelper {
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
