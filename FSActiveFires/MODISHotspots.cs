using Catfood.Shapefile;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net;

namespace FSActiveFires {
    class MODISHotspots {
        public Dictionary<string, string> datasets { get; private set; }
        public HashSet<Hotspot> hotspots { get; private set; }
        private string tempDirectory;
        private Log log;

        public MODISHotspots() {
            hotspots = new HashSet<Hotspot>();
            datasets = new Dictionary<string, string>();
            log = Log.Instance;

            datasets.Add("World", "https://firms.modaps.eosdis.nasa.gov/active_fire/{0}/Global_24h.{1}");
            datasets.Add("Alaska", "https://firms.modaps.eosdis.nasa.gov/active_fire/{0}/Alaska_24h.{1}");
            datasets.Add("Australia and New Zealand", "https://firms.modaps.eosdis.nasa.gov/active_fire/{0}/Australia_and_New_Zealand_24h.{1}");
            datasets.Add("Canada", "https://firms.modaps.eosdis.nasa.gov/active_fire/{0}/Canada_24h.{1}");
            datasets.Add("Central America", "https://firms.modaps.eosdis.nasa.gov/active_fire/{0}/Central_America_24h.{1}");
            datasets.Add("Europe", "https://firms.modaps.eosdis.nasa.gov/active_fire/{0}/Europe_24h.{1}");
            datasets.Add("Northern and Central Africa", "https://firms.modaps.eosdis.nasa.gov/active_fire/{0}/Northern_and_Central_Africa_24h.{1}");
            datasets.Add("Russia and Asia", "https://firms.modaps.eosdis.nasa.gov/active_fire/{0}/Russia_and_Asia_24h.{1}");
            datasets.Add("South America", "https://firms.modaps.eosdis.nasa.gov/active_fire/{0}/South_America_24h.{1}");
            datasets.Add("South Asia", "https://firms.modaps.eosdis.nasa.gov/active_fire/{0}/South_Asia_24h.{1}");
            datasets.Add("South East Asia", "https://firms.modaps.eosdis.nasa.gov/active_fire/{0}/SouthEast_Asia_24h.{1}");
            datasets.Add("Southern Africa", "https://firms.modaps.eosdis.nasa.gov/active_fire/{0}/Southern_Africa_24h.{1}");
            datasets.Add("U.S.A. (Conterminous) and Hawaii", "https://firms.modaps.eosdis.nasa.gov/active_fire/{0}/USA_contiguous_and_Hawaii_24h.{1}");

            tempDirectory = CreateTemporaryDirectory();
        }

        private string CreateTemporaryDirectory() {
            string tempDirectory = Path.Combine(Path.GetTempPath(), "FSActiveFires", Path.GetRandomFileName());
            log.Info(string.Format("Create temporary directory: {0}", tempDirectory));
            Directory.CreateDirectory(tempDirectory);
            return tempDirectory;
        }

        public void RemoveTemporaryDirectory() {
            string tempDirectoryRoot = Path.Combine(Path.GetTempPath(), "FSActiveFires");
            if (Directory.Exists(tempDirectoryRoot)) {
                log.Info(string.Format("Delete temporary directory: {0}", tempDirectoryRoot));
                Directory.Delete(tempDirectoryRoot, true);
            }
        }

        public void LoadData(string datasetFormatString) {
            try {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                LoadShapefileHotspots(DownloadShapefileData(datasetFormatString));
            }
            catch (System.Data.OleDb.OleDbException ex) {
                log.Warning(string.Format("Unable to parse shapefile; attempting to use CSV.\r\n{0}", ex.ToString()));
                LoadCsvHotspots(DownloadCsvData(datasetFormatString));
            }
        }

        private string DownloadShapefileData(string datasetFormatString) {
            string webUrl = string.Format(datasetFormatString, "shapes/zips", "zip");
            string zipFileName = webUrl.Substring(webUrl.LastIndexOf('/') + 1, webUrl.Length - webUrl.LastIndexOf('/') - 1);
            string zipFilePath = Path.Combine(tempDirectory, zipFileName);
            string shapefilePath = zipFilePath.Substring(0, zipFilePath.Length - 3) + "shp";

            if (File.Exists(shapefilePath)) {
                log.Info(string.Format("SHP already exists: {0}", shapefilePath));
                return shapefilePath;
            }

            using (WebClient webClient = new WebClient()) {
                log.Info(string.Format("Download ZIP: {0} -> {1}", webUrl, zipFilePath));
                webClient.DownloadFile(webUrl, zipFilePath);
            }

            if (!File.Exists(zipFilePath)) {
                throw new FileNotFoundException("ZIP file was not downloaded.");
            }

            log.Info(string.Format("Extract ZIP: {0} -> {1}", zipFilePath, tempDirectory));
            ZipFile.ExtractToDirectory(zipFilePath, tempDirectory);
            log.Info(string.Format("Delete ZIP: {0}", zipFilePath));
            File.Delete(zipFilePath);

            if (File.Exists(shapefilePath)) {
                return shapefilePath;
            }
            else {
                throw new FileNotFoundException("Shapefile was not downloaded or extracted.");
            }
        }

        private string DownloadCsvData(string datasetFormatString) {
            string webUrl = string.Format(datasetFormatString, "text", "csv");
            string fileName = webUrl.Substring(webUrl.LastIndexOf('/') + 1, webUrl.Length - webUrl.LastIndexOf('/') - 1);
            string filePath = Path.Combine(tempDirectory, fileName);

            if (File.Exists(filePath)) {
                log.Info(string.Format("CSV already exists: {0}", filePath));
                return filePath;
            }

            using (WebClient webClient = new WebClient()) {
                log.Info(string.Format("Download CSV: {0} -> {1}", webUrl, filePath));
                webClient.DownloadFile(webUrl, filePath);
            }

            if (File.Exists(filePath)) {
                return filePath;
            }
            else {
                throw new FileNotFoundException("CSV file was not downloaded.");
            }
        }

        private void LoadShapefileHotspots(string shapefilePath) {
            log.Info(string.Format("Parsing shapefile: {0}", shapefilePath));
            using (Shapefile shp = new Shapefile(shapefilePath)) {
                foreach (Shape shape in shp) {
                    if (shape.Type == ShapeType.Point) {
                        int confidence;
                        if (int.TryParse(shape.GetMetadata("confidence"), out confidence)) {
                            ShapePoint shapePoint = shape as ShapePoint;
                            hotspots.Add(new Hotspot(shapePoint.Point.Y, shapePoint.Point.X, confidence));
                        }
                    }
                }
            }
            log.Info(string.Format("Cumulative hotspots parsed: {0}", hotspots.Count));
        }

        private void LoadCsvHotspots(string csvPath) {
            log.Info(string.Format("Parsing CSV: {0}", csvPath));
            using (StreamReader sr = new StreamReader(csvPath)) {
                string line = sr.ReadLine();
                var fields = line.Split(',');
                if (fields[0].Equals("latitude") && fields[1].Equals("longitude") && fields[8].Equals("confidence")) {
                    while ((line = sr.ReadLine()) != null) {
                        fields = line.Split(',');
                        double lat;
                        double lon;
                        int confidence;
                        if (double.TryParse(fields[0], NumberStyles.Number, CultureInfo.InvariantCulture, out lat) &&
                            double.TryParse(fields[1], NumberStyles.Number, CultureInfo.InvariantCulture, out lon) &&
                            int.TryParse(fields[8], NumberStyles.Number, CultureInfo.InvariantCulture, out confidence)) {
                            hotspots.Add(new Hotspot(lat, lon, confidence));
                        }
                    }
                }
                else {
                    throw new FileFormatException("Unexpected data format.");
                }
            }
            log.Info(string.Format("Cumulative hotspots parsed: {0}", hotspots.Count));
        }
    }
}
