using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace FSActiveFires {
    class MainViewModel : NotifyPropertyChanged {
        private SimConnectInstance sc = null;
        private MODISHotspots activeFires;
        private Log log;

        public MainViewModel() {
            log = Log.Instance;
            log.Info("FS Active Fires by Orion Lyau\r\nVersion: " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version + "\r\n");

            activeFires = new MODISHotspots();
            SelectedDatasetUrl = activeFires.datasets["World"];

            sc = new SimConnectInstance();
            sc.PropertyChanged += (sender, args) => base.OnPropertyChanged(args.PropertyName);
        }

        #region Command Bindings

        private ICommand _connectCommand;
        public ICommand ConnectCommand {
            get {
                if (_connectCommand == null) {
                    _connectCommand = new RelayCommand(param => {
                        if (!IsConnected) {
                            sc.AddLocations(SimObjectTitle, activeFires.hotspots.Where(x => x.Confidence >= MinimumConfidence));
                            sc.Connect();
                        }
                        else {
                            sc.Disconnect();
                        }
                    });
                }
                return _connectCommand;
            }
        }

        private ICommand _downloadCommand;
        public ICommand DownloadCommand {
            get {
                if (_downloadCommand == null) {
                    _downloadCommand = new RelayCommand(param => {
#if !DEBUG
                        try {
#endif
                            activeFires.LoadData(SelectedDatasetUrl);
                            OnPropertyChanged("TotalFiresCount");
#if !DEBUG
                        }
                        catch (Exception ex) {
                            string message = string.Format("Message: {0}\r\nStack trace:\r\n{1}", ex.Message, ex.StackTrace);
                            log.Error(message);
                            System.Windows.MessageBox.Show(message, "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                        }
#endif
                    });
                }
                return _downloadCommand;
            }
        }

        private ICommand _relocateUserCommand;
        public ICommand RelocateUserCommand {
            get {
                if (_relocateUserCommand == null) {
                    _relocateUserCommand = new RelayCommand(param => {
                        sc.RelocateUserRandomly();
                    }, param => IsConnected);
                }
                return _relocateUserCommand;
            }
        }

        private ICommand _installCommand;
        public ICommand InstallCommand {
            get {
                if (_installCommand == null) {
                    _installCommand = new RelayCommand(param => {
#if !DEBUG
                        try {
#endif
                            FireEffect.InstallSimObject();
                            SimObjectTitle = "Fire_Effect";
#if !DEBUG
                        }
                        catch (Exception ex) {
                            string message = string.Format("Message: {0}\r\nStack trace:\r\n{1}", ex.Message, ex.StackTrace);
                            log.Error(message);
                            System.Windows.MessageBox.Show(message, "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                        }
#endif
                    });
                }
                return _installCommand;
            }
        }

        private ICommand _nasaCommand;
        public ICommand NASACommand {
            get {
                if (_nasaCommand == null) {
                    _nasaCommand = new RelayCommand(param => {
                        log.Info("Opening NASA website.");
                        System.Diagnostics.Process.Start("https://earthdata.nasa.gov/firms");
                    });
                }
                return _nasaCommand;
            }
        }

        private ICommand _closingCommand;
        public ICommand ClosingCommand {
            get {
                if (_closingCommand == null) {
                    _closingCommand = new RelayCommand(param => {
                        activeFires.RemoveTemporaryDirectory();
                        if (IsConnected) {
                            sc.Disconnect();
                        }
                        var args = Environment.GetCommandLineArgs();
                        if (args.Count() > 0) {
                            if (args[0].Equals("log", StringComparison.InvariantCultureIgnoreCase)) {
                                log.Save();
                            }
                        }
                        else {
                            log.SaveIfError();
                        }
                    });
                }
                return _closingCommand;
            }
        }

        #endregion

        #region Data Binding

        public bool IsConnected { get { return sc.IsConnected; } }
        public int CreatedSimObjectsCount { get { return sc.CreatedSimObjectsCount; } }
        public Dictionary<string, string> Datasets { get { return activeFires.datasets; } }
        public int TotalFiresCount { get { return activeFires.hotspots.Count; } }

        private string _selectedDatasetUrl;
        public string SelectedDatasetUrl {
            get { return _selectedDatasetUrl; }
            set { SetProperty(ref _selectedDatasetUrl, value); }
        }

        private string _simObjectTitle = "Fire_Effect"; // "Food_pallet"
        public string SimObjectTitle {
            get { return _simObjectTitle; }
            set { SetProperty(ref _simObjectTitle, value); }
        }

        private int _minimumConfidence;
        public int MinimumConfidence {
            get { return _minimumConfidence; }
            set { SetProperty(ref _minimumConfidence, value); }
        }

        #endregion
    }
}
