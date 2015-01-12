using iniLib;
using Microsoft.Win32;
using System.IO;
using System.Net;

namespace FSActiveFires {
    class FireEffect {
        public static void InstallSimObject(string simDirectory) {
            const string modelName = "Fire_Effect";
            const string modelWebLocation = "http://www.eaglerotorcraftsimulations.com/downloads/FireEffect.MDL";

            string[] directories = {
                                       Path.Combine(simDirectory, @"SimObjects\Misc\Fire_Effect"),
                                       Path.Combine(simDirectory, @"SimObjects\Misc\Fire_Effect\model")
                                   };

            string[] files = {
                                 Path.Combine(directories[0], "sim.cfg"),
                                 Path.Combine(directories[0], "model", "model.cfg"),
                                 Path.Combine(directories[0], "model", modelName + ".mdl")
                             };

            foreach (string directory in directories) {
                if (!Directory.Exists(directory)) {
                    Log.Instance.Info("Create directory: " + directory);
                    Directory.CreateDirectory(directory);
                }
            }

            foreach (string file in files) {
                if (File.Exists(file)) {
                    Log.Instance.Info("Delete file: " + file);
                    File.Delete(file);
                }
            }

            using (WebClient wc = new WebClient()) {
                Log.Instance.Info(string.Format("Download model: {0} -> {1}", modelWebLocation, files[2]));
                wc.DownloadFile(modelWebLocation, files[2]);
            }

            using (Ini simCfg = new Ini(files[0])) {
                Log.Instance.Info(string.Format("Write CFG: {0}", files[0]));
                simCfg.WriteKeyValue("fltsim.0", "title", modelName);
                simCfg.WriteKeyValue("fltsim.0", "model", "");
                simCfg.WriteKeyValue("fltsim.0", "texture", "");
                simCfg.WriteKeyValue("fltsim.0", "ui_type", "SceneryObject");
                simCfg.WriteKeyValue("General", "category", "Viewer");
                simCfg.WriteKeyValue("contact_points", "destroy_on_impact", "0");
            }

            using (Ini modelCfg = new Ini(files[1])) {
                Log.Instance.Info(string.Format("Write CFG: {0}", files[1]));
                modelCfg.WriteKeyValue("models", "normal", modelName);
            }
        }
    }
}
