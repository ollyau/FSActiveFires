using Catfood.Shapefile;
using iniLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace FSActiveFires {
    class MainViewModel : NotifyPropertyChanged {

        // Private fields

        private SimConnectInstance sc = null;
        private Dictionary<string, string> datasets;
        private HashSet<Coordinate> downloadedFires;

        private string workDirectory;
        private string selectedDataset;
        //private string simObjectTitle = "Food_pallet";
        private string simObjectTitle = "Fire_Effect";
        private string output;
        private int minimumConfidence;

        // UI Commands

        private ICommand _connectCommand;
        public ICommand ConnectCommand {
            get {
                if (_connectCommand == null) {
                    _connectCommand = new RelayCommand(param => Button_Connect_Click());
                }
                return _connectCommand;
            }
        }

        private ICommand _downloadCommand;
        public ICommand DownloadCommand {
            get {
                if (_downloadCommand == null) {
                    _downloadCommand = new RelayCommand(param => Button_Download_Click());
                }
                return _downloadCommand;
            }
        }

        private ICommand _relocateUserCommand;
        public ICommand RelocateUserCommand {
            get {
                if (_relocateUserCommand == null) {
                    _relocateUserCommand = new RelayCommand(param => Button_Relocate_User_Click(), param => IsConnected);
                }
                return _relocateUserCommand;
            }
        }

        private ICommand _installCommand;
        public ICommand InstallCommand {
            get {
                if (_installCommand == null) {
                    _installCommand = new RelayCommand(param => Button_Install_Click());
                }
                return _installCommand;
            }
        }

        private ICommand _nasaCommand;
        public ICommand NASACommand {
            get {
                if (_nasaCommand == null) {
                    _nasaCommand = new RelayCommand(param => Hyperlink_NASA_Click());
                }
                return _nasaCommand;
            }
        }

        private ICommand _closingCommand;
        public ICommand ClosingCommand {
            get {
                if (_closingCommand == null) {
                    _closingCommand = new RelayCommand(param => MainWindow_Closing());
                }
                return _closingCommand;
            }
        }

        // Expose SimConnect public properties for data binding

        public bool IsConnected { get { return sc.IsConnected; } }
        public bool LoggingEnabled { get { return sc.LoggingEnabled; } }
        public bool ObjectsCreated { get { return sc.ObjectsCreated; } }
        public int CreatedSimObjectsCount { get { return sc.CreatedSimObjectsCount; } }
        public string TextOutput { get { return sc.TextOutput; } }

        // Public properties for data binding

        public bool IsLogsChangedPropertyInViewModel {
            get { return sc.IsLogsChangedPropertyInViewModel; }
            set { sc.IsLogsChangedPropertyInViewModel = value; }
        }

        //public string Output {
        //    get { return output; }
        //    set { SetProperty(ref output, value); }
        //}

        public string SelectedDataset {
            get { return selectedDataset; }
            set { SetProperty(ref selectedDataset, value); }
        }
        public string SimObjectTitle {
            get { return simObjectTitle; }
            set { SetProperty(ref simObjectTitle, value); }
        }

        public int MinimumConfidence {
            get { return minimumConfidence; }
            set { SetProperty(ref minimumConfidence, value); }
        }

        public Dictionary<string, string> Datasets {
            get { return datasets; }
            set { SetProperty(ref datasets, value); }
        }

        public int TotalFiresCount {
            get { return downloadedFires.Count; }
        }

        // Private functions

        private string GetAppTempPath() {
            return Path.Combine(Path.GetTempPath(), "FSActiveFires");
        }

        private string CreateTemporaryDirectory() {
            string tempDirectory = Path.Combine(GetAppTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);
            return tempDirectory;
        }

        private void AddOutput(string text) {
            sc.AddOutput(text);
            //            if (LoggingEnabled) {
            //                Output += text + "\r\n";
            //                IsLogsChangedPropertyInViewModel = true;
            //#if DEBUG
            //                Console.WriteLine(text);
            //#endif
            //            }
        }

        /// <summary>
        /// Gets the Flight Simulator X root directory from the registry.
        /// </summary>
        /// <returns>String containing the Flight Simulator X root directory with ending slash.</returns>
        private string GetFsxDirectory() {
            string dir = (string)Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\microsoft games\flight simulator\10.0", "SetupPath", null);

            if (string.IsNullOrEmpty(dir)) {
                dir = (string)Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\microsoft games\flight simulator\10.0", "SetupPath", null);
            }

            if (!string.IsNullOrEmpty(dir) && !dir.EndsWith("\\")) {
                dir = dir.Insert(dir.Length, "\\");
            }

            return dir;
        }

        /// <summary>
        /// Installs the model that generates the fire effect
        /// </summary>
        private void InstallModel() {
            string fireEffectDir = GetFsxDirectory() + @"SimObjects\Misc\Fire_Effect";
            const string modelName = "Fire_Effect";
            const string modelWebLocation = "http://www.eaglerotorcraftsimulations.com/downloads/FireGenerator.MDL";

            // create the simobject dir if it's not there
            if (!Directory.Exists(fireEffectDir)) {
                Directory.CreateDirectory(fireEffectDir);
            }
            // create the model dir if it's not there
            if (!Directory.Exists(fireEffectDir + "\\model")) {
                Directory.CreateDirectory(fireEffectDir + "\\model");
            }

            // delete the existing sim.cfg if it's already there
            if (File.Exists(fireEffectDir + "\\sim.cfg")) {
                File.Delete(fireEffectDir + "\\sim.cfg");
            }
            // delete the model.cfg if it's already there
            if (File.Exists(fireEffectDir + "\\model\\model.cfg")) {
                File.Delete(fireEffectDir + "\\model\\model.cfg");
            }
            // delete the existing model if it's already there
            if (File.Exists(fireEffectDir + "\\model\\" + modelName + ".mdl")) {
                File.Delete(fireEffectDir + "\\model\\" + modelName + ".mdl");
            }

            // download the model
            using (System.Net.WebClient wc = new System.Net.WebClient()) {
                wc.DownloadFile(modelWebLocation, fireEffectDir + "\\model\\" + modelName + ".mdl");
            }

            // write the sim.cfg file
            using (Ini simCfg = new Ini(fireEffectDir + "\\sim.cfg")) {
                simCfg.WriteKeyValue("fltsim.0", "title", modelName);
                simCfg.WriteKeyValue("fltsim.0", "model", "");
                simCfg.WriteKeyValue("fltsim.0", "texture", "");
                simCfg.WriteKeyValue("fltsim.0", "ui_type", "SceneryObject");
                simCfg.WriteKeyValue("General", "category", "SimpleObject");
                simCfg.WriteKeyValue("contact_points", "destroy_on_impact", "0");
            }

            // write the model.cfg file
            using (Ini modelCfg = new Ini(fireEffectDir + "\\model\\model.cfg")) {
                modelCfg.WriteKeyValue("models", "normal", modelName);
            }

            System.Windows.MessageBox.Show("Fire effect SimObject now installed.", "SimObject Installed", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        // Class constructor

        public MainViewModel() {

            // Initialize the datasets
            datasets = new Dictionary<string, string>();
            datasets.Add("World", "https://firms.modaps.eosdis.nasa.gov/active_fire/shapes/zips/Global_24h.zip");
            datasets.Add("Alaska", "https://firms.modaps.eosdis.nasa.gov/active_fire/shapes/zips/Alaska_24h.zip");
            datasets.Add("Australia and New Zealand", "https://firms.modaps.eosdis.nasa.gov/active_fire/shapes/zips/Australia_and_New_Zealand_24h.zip");
            datasets.Add("Canada", "https://firms.modaps.eosdis.nasa.gov/active_fire/shapes/zips/Canada_24h.zip");
            datasets.Add("Central America", "https://firms.modaps.eosdis.nasa.gov/active_fire/shapes/zips/Central_America_24h.zip");
            datasets.Add("Europe", "https://firms.modaps.eosdis.nasa.gov/active_fire/shapes/zips/Europe_24h.zip");
            datasets.Add("Northern and Central Africa", "https://firms.modaps.eosdis.nasa.gov/active_fire/shapes/zips/Northern_and_Central_Africa_24h.zip");
            datasets.Add("Russia and Asia", "https://firms.modaps.eosdis.nasa.gov/active_fire/shapes/zips/Russia_and_Asia_24h.zip");
            datasets.Add("South America", "https://firms.modaps.eosdis.nasa.gov/active_fire/shapes/zips/South_America_24h.zip");
            datasets.Add("South Asia", "https://firms.modaps.eosdis.nasa.gov/active_fire/shapes/zips/South_Asia_24h.zip");
            datasets.Add("South East Asia", "https://firms.modaps.eosdis.nasa.gov/active_fire/shapes/zips/SouthEast_Asia_24h.zip");
            datasets.Add("Southern Africa", "https://firms.modaps.eosdis.nasa.gov/active_fire/shapes/zips/Southern_Africa_24h.zip");
            datasets.Add("U.S.A. (Conterminous) and Hawaii", "https://firms.modaps.eosdis.nasa.gov/active_fire/shapes/zips/USA_contiguous_and_Hawaii_24h.zip");

            SelectedDataset = datasets["World"];

            // Instantiate list
            downloadedFires = new HashSet<Coordinate>();

            // Get a temporary directory
            workDirectory = CreateTemporaryDirectory();

            // Initialize SimConnect
            sc = new SimConnectInstance();

            // Subscribe to property changed events in sc, and re-trigger them here
            sc.PropertyChanged += (sender, args) => base.OnPropertyChanged(args.PropertyName);
        }

        private void ProcessData(string webUrl) {
            // Get destination file path
            string fileName = webUrl.Substring(webUrl.LastIndexOf('/') + 1, webUrl.Length - webUrl.LastIndexOf('/') - 1);
            string downloadDestination = workDirectory + "\\" + fileName;

            // Get expected shapefile path
            string shapefileName = downloadDestination.Substring(0, downloadDestination.Length - 3) + "shp";

            // Check if the relevant shapefile was already downloaded
            if (!File.Exists(shapefileName)) {
                // Inform user
                AddOutput("Destination: " + downloadDestination);

                // Download file
                using (System.Net.WebClient webClient = new System.Net.WebClient()) {
                    AddOutput("Downloading file: " + fileName);
                    webClient.DownloadFile(webUrl, downloadDestination);
                }

                // Extract files
                AddOutput("Extracting file: " + fileName);
                ZipFile.ExtractToDirectory(downloadDestination, workDirectory);

                // Delete zip
                if (File.Exists(downloadDestination)) {
                    AddOutput("Deleting file: " + fileName);
                    File.Delete(downloadDestination);
                }

                // Parse shapefile            
                ParseShapefile(shapefileName);

                // Add locations to list for SimConnect to use
                sc.AddLocations(SimObjectTitle, downloadedFires.ToArray());
            }
        }

        private void ParseShapefile(string pathToShapefile) {
            // based on Catfood.Shapefile demo app
            using (Shapefile shp = new Shapefile(pathToShapefile)) {

                //AddOutput(String.Format("Type: {0}, Shapes: {1:n0}", shp.Type, shp.Count));

                // Loop through app the shapes in the shapefile
                foreach (Shape shape in shp) {
                    //AddOutput(String.Format("Shape {0:n0}, Type {1}", shape.RecordNumber, shape.Type));

                    //// Get the metadata from the shape
                    //string[] metadataNames = shape.GetMetadataNames();
                    //if (metadataNames != null) {
                    //    AddOutput(String.Format("Metadata:"));
                    //    foreach (string metadataName in metadataNames) {
                    //        AddOutput(String.Format("{0}={1} ({2})", metadataName, shape.GetMetadata(metadataName), shape.DataRecord.GetDataTypeName(shape.DataRecord.GetOrdinal(metadataName))));
                    //    }
                    //    AddOutput("\r\n");
                    //}

                    // Cast shape based on the type
                    switch (shape.Type) {
                        case ShapeType.Point:
                            // Get the metadata from the shape
                            int confidence;
                            if (int.TryParse(shape.GetMetadata("confidence"), out confidence)) {
                                if (confidence >= MinimumConfidence) {
                                    ShapePoint shapePoint = shape as ShapePoint;

                                    // add fire to list of fires if it doesn't exist already
                                    downloadedFires.Add(new Coordinate(shapePoint.Point.Y, shapePoint.Point.X));

                                    // Raise event to update TotalFiresCount property to update GUI
                                    OnPropertyChanged("TotalFiresCount");
                                }
                            }
                            break;
                        //default:
                        //    //AddOutput("Shape type " + Enum.GetName(typeof(ShapeType), shape.Type) + " is not supported.");
                        //    break;
                    }
                    //AddOutput("\r\n");
                }
            }
        }

        // Command logic

        private void Button_Download_Click() {
            ProcessData(SelectedDataset);
        }

        private void MainWindow_Closing() {
            if (Directory.Exists(GetAppTempPath())) {
                AddOutput("Delete directory: " + GetAppTempPath());
                Directory.Delete(GetAppTempPath(), true);
            }
            if (IsConnected) {
                sc.Disconnect();
            }
        }

        private void Button_Relocate_User_Click() {
            sc.RelocateUserRandomly(downloadedFires.ToArray());
        }

        private void Button_Connect_Click() {
            if (!IsConnected) {
                sc.Connect();
            }
            else {
                sc.Disconnect();
            }
        }

        //internal void Button_CreateObjects_Click(object sender, System.Windows.RoutedEventArgs e) {
        //    sc.CreateAllObjects(SimObjectTitle, downloadedFires.ToArray());
        //}

        //internal void Button_RemoveObjects_Click(object sender, System.Windows.RoutedEventArgs e) {
        //    sc.RemoveAllObjects();
        //}

        private void Button_Install_Click() {
            // Install model
#if !DEBUG
            try {
#endif
            InstallModel();
#if !DEBUG
            }
            catch (Exception ex) {
                System.Windows.MessageBox.Show("Unable to install SimObject.\r\n\r\n" + ex.Message, "Error Installing", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                if (Directory.Exists(GetFsxDirectory() + @"SimObjects\Misc\Fire_Effect")) {
                    Directory.Delete(GetFsxDirectory() + @"SimObjects\Misc\Fire_Effect", true);
                }
            }
#endif
        }

        private void Hyperlink_NASA_Click() {
            System.Diagnostics.Process.Start("https://earthdata.nasa.gov/firms");
        }
    }
}
