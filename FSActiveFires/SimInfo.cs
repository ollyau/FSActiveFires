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
    class SimInfo {
        private static string fsxDirectory;
        private static string espDirectory;
        private static string p3dDirectory;
        private static string p3d2Directory;

        private static string fsxVersion;
        private static string espVersion;
        private static string p3dVersion;
        private static string p3d2Version;

        private const string FSX_REG_KEY = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\microsoft games\flight simulator\10.0";
        private const string ESP_REG_KEY = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Microsoft ESP\1.0";
        private const string P3D_REG_KEY = @"HKEY_LOCAL_MACHINE\SOFTWARE\LockheedMartin\Prepar3D";
        private const string P3D2_REG_KEY = @"HKEY_LOCAL_MACHINE\SOFTWARE\Lockheed Martin\Prepar3D v2";

        private const string NOT_FOUND = "NOT_FOUND";

        private static string GetSimDirectory(string regKey, ref string simDirectory) {
            simDirectory = (string)Registry.GetValue(regKey, "SetupPath", null);

            if (string.IsNullOrEmpty(simDirectory)) {
                simDirectory = (string)Registry.GetValue(regKey.Insert("HKEY_LOCAL_MACHINE\\SOFTWARE\\".Length, "Wow6432Node\\"), "SetupPath", null);
            }

            if (!string.IsNullOrEmpty(simDirectory) && !simDirectory.EndsWith("\\")) {
                simDirectory = simDirectory.Insert(simDirectory.Length, "\\");
            }

            if (string.IsNullOrEmpty(simDirectory)) {
                simDirectory = NOT_FOUND;
            }

            return simDirectory;
        }

        private static string GetSimVersion(string simDirectory, string simExecutable, ref string simVersion) {
            string simExePath = Path.Combine(simDirectory, simExecutable);
            if (File.Exists(simExePath)) {
                simVersion = FileVersionInfo.GetVersionInfo(simExePath).ProductVersion;
                return simVersion;
            }
            else {
                return NOT_FOUND;
            }
        }

        private static string FsxVersion {
            get { return string.IsNullOrEmpty(fsxVersion) ? GetSimVersion(FsxDirectory, "fsx.exe", ref fsxVersion) : fsxVersion; } 
        }

        private static string EspVersion {
            get { return string.IsNullOrEmpty(espVersion) ? GetSimVersion(EspDirectory, "esp.exe", ref espVersion) : espVersion; }
        }

        private static string P3dVersion {
            get { return string.IsNullOrEmpty(p3dVersion) ? GetSimVersion(p3dDirectory, "Prepar3D.exe", ref p3dVersion) : p3dVersion; }
        }

        private static string P3d2Version {
            get { return string.IsNullOrEmpty(p3d2Version) ? GetSimVersion(p3d2Directory, "Prepar3D.exe", ref p3d2Version) : p3d2Version; }
        }

        private static string FsxDirectory {
            get { return string.IsNullOrEmpty(fsxDirectory) ? GetSimDirectory(FSX_REG_KEY, ref fsxDirectory) : fsxDirectory; }
        }

        private static string EspDirectory {
            get { return string.IsNullOrEmpty(espDirectory) ? GetSimDirectory(ESP_REG_KEY, ref espDirectory) : espDirectory; }
        }

        private static string P3dDirectory {
            get { return string.IsNullOrEmpty(p3dDirectory) ? GetSimDirectory(P3D_REG_KEY, ref p3dDirectory) : p3dDirectory; }
        }

        private static string P3d2Directory {
            get { return string.IsNullOrEmpty(p3d2Directory) ? GetSimDirectory(P3D2_REG_KEY, ref p3d2Directory) : p3d2Directory; }
        }

        private static bool FsxRunning {
            get { return FsxDirectory.Equals(NOT_FOUND) ? false : Process.GetProcessesByName("fsx").Any(x => x.MainModule.FileName == Path.Combine(FsxDirectory, "fsx.exe")); } 
        }

        private static bool EspRunning {
            get { return EspDirectory.Equals(NOT_FOUND) ? false : Process.GetProcessesByName("esp").Any(x => x.MainModule.FileName == Path.Combine(EspDirectory, "esp.exe")); }
        }

        private static bool P3dRunning {
            get { return P3dDirectory.Equals(NOT_FOUND) ? false : Process.GetProcessesByName("Prepar3D").Any(x => x.MainModule.FileName == Path.Combine(P3dDirectory, "Prepar3D.exe")); }
        }

        private static bool P3d2Running {
            get { return P3d2Directory.Equals(NOT_FOUND) ? false : Process.GetProcessesByName("Prepar3D").Any(x => x.MainModule.FileName == Path.Combine(P3d2Directory, "Prepar3D.exe")); }
        }

        private static bool FSXCompatibility {
            get {
                return !((!SimInfo.FsxVersion.Equals("10.0.61637.0 (FSX-Xpack.20070926-1421)") && !SimInfo.FsxVersion.Equals("10.0.61472.0 (fsx-sp2.20071210-2023)")));
            }
        }

        public static bool IncompatibleFSXRunning {
            get { return SimInfo.FsxRunning && !SimInfo.FSXCompatibility; }
        }

        public static IEnumerable<string> SimDirectories {
            get {
                string[] allSims = { SimInfo.FsxDirectory, SimInfo.EspDirectory, SimInfo.P3dDirectory, SimInfo.P3d2Directory };
                return allSims.Where(x => !string.IsNullOrEmpty(x) && !x.Equals(NOT_FOUND));
            }
        }

        public static void LogSimInfo() {
            Log log = Log.Instance;
            const string data = "Simulator information:\r\nFSX Directory: {0}\r\nESP Directory: {1}\r\nP3D Directory: {2}\r\nP3D2 Directory: {3}\r\nFSX Version: {4}\r\nESP Version: {5}\r\nP3D Version: {6}\r\nP3D2 Version: {7}\r\nFSX Running: {8}\r\nESP Running: {9}\r\nP3D Running: {10}\r\nP3D2 Running: {11}";
            log.Info(string.Format(data, FsxDirectory, EspDirectory, P3dDirectory, P3d2Directory, FsxVersion, EspVersion, P3dVersion, P3d2Version, FsxRunning, EspRunning, P3dRunning, P3d2Running));
            log.Info(string.Format("Compatible FSX version (Acceleration or SP2): {0}", FSXCompatibility));
            log.Info(string.Format("Incompatible version of FSX running: {0}", IncompatibleFSXRunning));
            log.Info(string.Format("Directories of currently installed simulators:\r\n{0}", string.Join("\r\n", SimDirectories.ToArray())));
            log.ShouldSave = true;
        }
    }
}
