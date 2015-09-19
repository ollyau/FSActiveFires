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
    class Simulator {
        readonly string _registryKey;
        readonly string _registryValue;
        readonly string _executableName;

        private string _directory;
        private FileVersionInfo _versionInfo;

        public Simulator(string executableName, string registryKey, string registryValue = "SetupPath") {
            _executableName = executableName;
            _registryKey = registryKey;
            _registryValue = registryValue;
        }

        public string Directory {
            get { return string.IsNullOrEmpty(_directory) ? GetDirectory() : _directory; }
        }

        public FileVersionInfo VersionInfo {
            get { return _versionInfo == null ? GetVersion() : _versionInfo; }
        }

        private string GetDirectory() {
            _directory = (string)Registry.GetValue(_registryKey, _registryValue, null);

            if (string.IsNullOrEmpty(_directory)) {
                _directory = (string)Registry.GetValue(_registryKey.Insert("HKEY_LOCAL_MACHINE\\SOFTWARE\\".Length, "Wow6432Node\\"), _registryValue, null);
            }

            if (!string.IsNullOrEmpty(_directory) && !_directory.EndsWith("\\")) {
                _directory = _directory.Insert(_directory.Length, "\\");
            }

            return _directory;
        }

        private FileVersionInfo GetVersion() {
            var dir = Directory;
            if (!string.IsNullOrEmpty(dir)) {
                string simExePath = Path.Combine(dir, _executableName + ".exe");
                if (File.Exists(simExePath)) {
                    _versionInfo = FileVersionInfo.GetVersionInfo(simExePath);
                    return _versionInfo;
                }
            }
            return null;
        }

        public bool Running {
            get {
                try {
                    return string.IsNullOrEmpty(Directory) ? false : Process.GetProcessesByName(_executableName).Any(x => x.MainModule.FileName == Path.Combine(Directory, _executableName + ".exe"));
                }
                catch (System.ComponentModel.Win32Exception) {
                    // unable to determine
                    return false;
                }
            }
        }
    }

    class SimInfo {
        private static readonly Lazy<SimInfo> InfoInstance = new Lazy<SimInfo>(() => new SimInfo());
        public static SimInfo Instance { get { return InfoInstance.Value; } }

        private List<Simulator> simulators;
        private bool? _fsxCompatibility;

        private SimInfo() {
            simulators = new List<Simulator>()
            {
                new Simulator("fsx", @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\microsoft games\flight simulator\10.0"),
                new Simulator("esp", @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Microsoft ESP\1.0"),
                new Simulator("Prepar3D", @"HKEY_LOCAL_MACHINE\SOFTWARE\LockheedMartin\Prepar3D"),
                new Simulator("Prepar3D", @"HKEY_LOCAL_MACHINE\SOFTWARE\Lockheed Martin\Prepar3D v2"),
                new Simulator("fsx", @"HKEY_LOCAL_MACHINE\SOFTWARE\DovetailGames\FSX", "install_path")
            };
        }

        private bool GetFsxCompatibility() {
            var simVersion = simulators[0].VersionInfo;
            _fsxCompatibility = simVersion.FileMajorPart == 10 && simVersion.FileMinorPart == 0 && (simVersion.FileBuildPart == 61637 || simVersion.FileBuildPart == 61472 || simVersion.FileBuildPart >= 62608);
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
                return simDirs.Where(x => !string.IsNullOrEmpty(x));
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
