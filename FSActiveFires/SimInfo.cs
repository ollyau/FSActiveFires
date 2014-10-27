using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSActiveFires {
    class SimInfo {
        private static string fsxDirectory;
        private static string p3dDirectory;
        private static string p3d2Directory;

        const string FSX_REG_KEY = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\microsoft games\flight simulator\10.0";
        const string P3D_REG_KEY = @"HKEY_LOCAL_MACHINE\SOFTWARE\LockheedMartin\Prepar3D";
        const string P3D2_REG_KEY = @"HKEY_LOCAL_MACHINE\SOFTWARE\Lockheed Martin\Prepar3D v2";

        private static string GetSimDirectory(string regKey, ref string simDirectory) {
            simDirectory = (string)Registry.GetValue(regKey, "SetupPath", null);

            if (string.IsNullOrEmpty(simDirectory)) {
                simDirectory = (string)Registry.GetValue(regKey.Insert("HKEY_LOCAL_MACHINE\\SOFTWARE\\".Length, "Wow6432Node\\"), "SetupPath", null);
            }

            if (!string.IsNullOrEmpty(simDirectory) && !simDirectory.EndsWith("\\")) {
                simDirectory = simDirectory.Insert(simDirectory.Length, "\\");
            }

            return simDirectory;
        }

        public static string FsxDirectory {
            get { return string.IsNullOrEmpty(fsxDirectory) ? GetSimDirectory(FSX_REG_KEY, ref fsxDirectory) : fsxDirectory; }
        }

        public static string P3dDirectory {
            get { return string.IsNullOrEmpty(p3dDirectory) ? GetSimDirectory(P3D_REG_KEY, ref p3dDirectory) : p3dDirectory; }
        }

        public static string P3d2Directory {
            get { return string.IsNullOrEmpty(p3d2Directory) ? GetSimDirectory(P3D2_REG_KEY, ref p3d2Directory) : p3d2Directory; }
        }
    }
}
