using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSActiveFires {
    abstract class Simulator {
        public const string NOT_FOUND = "NOT_FOUND";

        protected string _registryKey;
        protected string _registryValue;
        protected string _executableName;

        private string _directory;
        private string _versionInfo;

        public Simulator() {
            _registryValue = "SetupPath";
        }

        private string GetDirectory() {
            _directory = (string)Registry.GetValue(_registryKey, _registryValue, null);

            if (string.IsNullOrEmpty(_directory)) {
                _directory = (string)Registry.GetValue(_registryKey.Insert("HKEY_LOCAL_MACHINE\\SOFTWARE\\".Length, "Wow6432Node\\"), _registryValue, null);
            }

            if (!string.IsNullOrEmpty(_directory) && !_directory.EndsWith("\\")) {
                _directory = _directory.Insert(_directory.Length, "\\");
            }

            if (string.IsNullOrEmpty(_directory)) {
                _directory = NOT_FOUND;
            }

            return _directory;
        }

        private string GetVersion() {
            string simExePath = Path.Combine(Directory, _executableName + ".exe");
            if (File.Exists(simExePath)) {
                _versionInfo = FileVersionInfo.GetVersionInfo(simExePath).ProductVersion;
                return _versionInfo;
            }
            else {
                return NOT_FOUND;
            }
        }

        public string VersionInfo {
            get { return string.IsNullOrEmpty(_versionInfo) ? GetVersion() : _versionInfo; }
        }

        public string Directory {
            get { return string.IsNullOrEmpty(_directory) ? GetDirectory() : _directory; }
        }

        public bool Running {
            get {
                try {
                    return Directory.Equals(NOT_FOUND) ? false : Process.GetProcessesByName(_executableName).Any(x => x.MainModule.FileName == Path.Combine(Directory, _executableName + ".exe"));
                }
                catch (System.ComponentModel.Win32Exception) {
                    // unable to determine
                    return false;
                }
            }
        }
    }

    class FlightSimulatorX : Simulator { public FlightSimulatorX() { _registryKey = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\microsoft games\flight simulator\10.0"; _executableName = "fsx"; } }
    class EnterpriseSimulationPlatform : Simulator { public EnterpriseSimulationPlatform() { _registryKey = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Microsoft ESP\1.0"; _executableName = "esp"; } }
    class Prepar3D : Simulator { public Prepar3D() { _registryKey = @"HKEY_LOCAL_MACHINE\SOFTWARE\LockheedMartin\Prepar3D"; _executableName = "Prepar3D"; } }
    class Prepar3D2 : Simulator { public Prepar3D2() { _registryKey = @"HKEY_LOCAL_MACHINE\SOFTWARE\Lockheed Martin\Prepar3D v2"; _executableName = "Prepar3D"; } }
    class FlightSimulatorXSteamEdition : Simulator { public FlightSimulatorXSteamEdition() { _registryKey = @"HKEY_LOCAL_MACHINE\SOFTWARE\DovetailGames\FSX"; _executableName = "fsx"; _registryValue = "install_path"; } }

    class SimInfo {
        private static readonly Lazy<SimInfo> InfoInstance = new Lazy<SimInfo>(() => new SimInfo());
        public static SimInfo Instance { get { return InfoInstance.Value; } }

        private List<Simulator> simulators;
        private bool? _fsxCompatibility;

        private SimInfo() {
            simulators = new List<Simulator>();
            simulators.Add(new FlightSimulatorX());
            simulators.Add(new EnterpriseSimulationPlatform());
            simulators.Add(new Prepar3D());
            simulators.Add(new Prepar3D2());
            simulators.Add(new FlightSimulatorXSteamEdition());
        }

        private bool GetFsxCompatibility() {
            _fsxCompatibility = !(!simulators[0].VersionInfo.Equals("10.0.61637.0 (FSX-Xpack.20070926-1421)") && !simulators[0].VersionInfo.Equals("10.0.61472.0 (fsx-sp2.20071210-2023)"));
            return (bool)_fsxCompatibility;
        }

        private bool FSXCompatibility {
            get { return _fsxCompatibility == null ? GetFsxCompatibility() : (bool)_fsxCompatibility; }
        }

        public bool IncompatibleFSXRunning {
            get { return !FSXCompatibility && simulators[0].Running; }
        }

        public IEnumerable<string> SimDirectories {
            get {
                List<string> simDirs = new List<string>();
                foreach (Simulator s in simulators) {
                    simDirs.Add(s.Directory);
                }
                return simDirs.Where(x => !string.IsNullOrEmpty(x) && !x.Equals(Simulator.NOT_FOUND));
            }
        }

        public void LogSimInfo() {
            Log log = Log.Instance;
            foreach (Simulator s in simulators) {
                Type type = s.GetType();
                System.Reflection.PropertyInfo[] properties = type.GetProperties();
                foreach (System.Reflection.PropertyInfo property in properties) {
                    log.Info(string.Format("{0}.{1}: {2}", type.ToString(), property.Name, property.GetValue(s, null)));
                }
            }
            log.Info(string.Format("Compatible FSX version (Acceleration or SP2): {0}", FSXCompatibility));
            log.Info(string.Format("Incompatible version of FSX running: {0}", IncompatibleFSXRunning));
            log.Info(string.Format("Directories of currently installed simulators:\r\n{0}", string.Join("\r\n", SimDirectories.ToArray())));
            log.ShouldSave = true;
        }
    }
}
