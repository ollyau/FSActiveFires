using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace FSActiveFires {
    class MainViewModel : NotifyPropertyChanged {
        private SimConnectInstance sc = null;
        private MODISHotspots activeFires;
        private Log log;

        public MainViewModel() {
            log = Log.Instance;

            activeFires = new MODISHotspots();
            SelectedDatasetUrl = activeFires.datasets["World"];

            sc = new SimConnectInstance();
            sc.PropertyChanged += (sender, args) => base.OnPropertyChanged(args.PropertyName);

            Task.Run(async () => {
                CanExecute = false;
                await CheckArguments();
                CanExecute = true;
            });
        }

        private async Task CheckArguments() {
            await Task.Run(() => {
                var parser = new ArgumentParser();
                parser.Check("log", () => { Log.Instance.ShouldSave = true; });

                parser.Check("title", (arg) => {
                    SimObjectTitle = arg;
                    log.Info(string.Format("Title argument: {0}", arg));
                });

                parser.Check("confidence", (arg) => {
                    int val = 0;
                    int.TryParse(arg, out val);
                    if (val > 100) { MinimumConfidence = 100; }
                    else if (val < 0) { MinimumConfidence = 0; }
                    else { MinimumConfidence = val; }
                    log.Info(string.Format("Confidence argument: {0} parsed: {1}", arg, MinimumConfidence));
                });

                parser.Check("download", (arg) => {
                    log.Info(string.Format("Download argument: {0}", arg));
                    if (Datasets.ContainsKey(arg)) {
                        SelectedDatasetUrl = Datasets[arg];
                    }
                    else {
                        log.Warning(string.Format("Unknown dataset: \"{0}\".  Defaulting to World.", arg));
                    }
                    activeFires.LoadData(SelectedDatasetUrl);
                    OnPropertyChanged("TotalFiresCount");
                });

                parser.Check("connect", () => {
                    log.Info(string.Format("Connect argument"));
                    log.Info(string.Format("Minimum detection confidence: {0}%", MinimumConfidence));
                    if (SimInfo.Instance.IncompatibleFSXRunning) {
                        throw new NotSupportedException("FS Active Fires is only compatible with Microsoft Flight Simulator X: Acceleration and SP2.");
                    }
                    sc.AddLocations(SimObjectTitle, activeFires.hotspots.Where(x => x.Confidence >= MinimumConfidence));
                    sc.Connect();
                });
            });
        }

        #region Command Bindings

        private ICommand _installCommand;
        public ICommand InstallCommand {
            get {
                if (_installCommand == null) {
                    _installCommand = new RelayCommandAsync(async _ => {
#if !DEBUG
                        try {
#endif
                            log.Info("InstallCommand");
                            await Task.Run(() => {
                                var simDirs = SimInfo.Instance.SimDirectories;
                                if (simDirs.Count() > 0) {
                                    foreach (var sim in simDirs) {
                                        FireEffect.InstallSimObject(sim);
                                    }
                                }
                                else {
                                    throw new DirectoryNotFoundException("No simulator directories found.");
                                }
                            });
                            SimObjectTitle = "Fire_Effect";
#if !DEBUG
                        }
                        catch (Exception ex) {
                            log.Error(ex.ToString());
                            System.Windows.MessageBox.Show(string.Format("Error while installing SimObject.\r\n{0}", ex.Message), "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                        }
#endif
                    });
                }
                return _installCommand;
            }
        }

        private ICommand _downloadCommand;
        public ICommand DownloadCommand {
            get {
                if (_downloadCommand == null) {
                    _downloadCommand = new RelayCommandAsync(async _ => {
#if !DEBUG
                        try {
#endif
                            log.Info("DownloadCommand");
                            await Task.Run(() => {
                                activeFires.LoadData(SelectedDatasetUrl);
                            });
                            OnPropertyChanged("TotalFiresCount");
#if !DEBUG
                        }
                        catch (Exception ex) {
                            log.Error(ex.ToString());
                            System.Windows.MessageBox.Show(string.Format("Error while downloading data.\r\n{0}", ex.Message), "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                        }
#endif
                    });
                }
                return _downloadCommand;
            }
        }

        private ICommand _connectCommand;
        public ICommand ConnectCommand {
            get {
                if (_connectCommand == null) {
                    _connectCommand = new RelayCommandAsync(async _ => {
                        log.Info("ConnectCommand");
                        if (!IsConnected) {
                            log.Info(string.Format("Minimum detection confidence: {0}%", MinimumConfidence));
                            await Task.Run(() => {
                                if (SimInfo.Instance.IncompatibleFSXRunning) {
                                    throw new NotSupportedException("FS Active Fires is only compatible with Microsoft Flight Simulator X: Acceleration and SP2.");
                                }
                                sc.AddLocations(SimObjectTitle, activeFires.hotspots.Where(x => x.Confidence >= MinimumConfidence));
                                sc.Connect();
                            });
                        }
                        else {
                            await Task.Run(() => {
                                sc.Disconnect();
                            });
                        }
                    });
                }
                return _connectCommand;
            }
        }

        private ICommand _relocateUserCommand;
        public ICommand RelocateUserCommand {
            get {
                if (_relocateUserCommand == null) {
                    _relocateUserCommand = new RelayCommandAsync(async _ => {
                        log.Info("RelocateUserCommand");
                        await Task.Run(() => {
                            sc.RelocateUserRandomly();
                        });
                    });
                }
                return _relocateUserCommand;
            }
        }

        private ICommand _nasaCommand;
        public ICommand NASACommand {
            get {
                if (_nasaCommand == null) {
                    _nasaCommand = new RelayCommand(param => {
                        log.Info("NASACommand");
                        System.Diagnostics.Process.Start("https://earthdata.nasa.gov/firms");
                    });
                }
                return _nasaCommand;
            }
        }

        private ICommand _donateCommand;
        public ICommand DonateCommand {
            get {
                if (_donateCommand == null) {
                    _donateCommand = new RelayCommand(param => {
                        log.Info("DonateCommand");
                        System.Diagnostics.Process.Start("https://www.paypal.com/cgi-bin/webscr?cmd=_xclick&business=orion%2epublic%40live%2ecom&lc=US&item_name=FS%20Active%20Fires%20Tip&button_subtype=services&no_note=0&currency_code=USD");
                    });
                }
                return _donateCommand;
            }
        }

        private ICommand _closingCommand;
        public ICommand ClosingCommand {
            get {
                if (_closingCommand == null) {
                    _closingCommand = new RelayCommand(param => {
#if !DEBUG
                        try {
#endif
                            activeFires.RemoveTemporaryDirectory();
#if !DEBUG
                        }
                        catch (Exception ex) {
                            log.Warning(string.Format("Unable to remove temporary directory.\r\nType: {0}\r\nMessage: {1}\r\nStack trace:\r\n{2}", ex.GetType(), ex.Message, ex.StackTrace));
                        }
#endif
                        if (IsConnected) {
                            sc.Disconnect();
                        }
                        log.ConditionalSave();
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

        private bool _canExecute = true;
        public bool CanExecute {
            get { return _canExecute; }
            set { SetProperty(ref _canExecute, value); }
        }

        #endregion
    }
}
