using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace FSActiveFires {
    class Log {
        private static readonly Lazy<Log> LogInstance = new Lazy<Log>(() => new Log());
        public static Log Instance { get { return LogInstance.Value; } }

        public bool ShouldSave = false;

        private string AssemblyLoadDirectory {
            get { return Path.GetDirectoryName(new System.Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).LocalPath); }
        }

        private DateTime GetLinkerTimeStamp() {
            const int headerOffset = 60;
            const int timeStampOffset = 8;
            byte[] data = new byte[2048];
            using (var s = new FileStream(Assembly.GetExecutingAssembly().Location, FileMode.Open, FileAccess.Read)) {
                s.Read(data, 0, 2048);
            }
            int dataOffset = BitConverter.ToInt32(data, headerOffset);
            uint unixTime = BitConverter.ToUInt32(data, dataOffset + timeStampOffset);
            return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(unixTime);
        }

        private StringBuilder logData;

        private Log() {
            logData = new StringBuilder();
            logData.AppendLine(string.Format("Program built at {0} (version {1})\r\nLogging enabled at {2}\r\n",
                GetLinkerTimeStamp().ToString("yyyy-MM-ddTHH:mm:ssZ"),
                Assembly.GetExecutingAssembly().GetName().Version,
                DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz")));
        }

        public void WriteLine(string s) {
            System.Diagnostics.Debug.WriteLine(s);
            logData.AppendLine(s);
        }

        public void Info(string s) {
            string message = string.Format("{0} Info {1}", DateTime.Now.ToString("HH:mm:ss.fff"), s);
            System.Diagnostics.Debug.WriteLine(message);
            logData.AppendLine(message);
        }

        public void Warning(string s) {
            string message = string.Format("{0} Warning {1}", DateTime.Now.ToString("HH:mm:ss.fff"), s);
            System.Diagnostics.Debug.WriteLine(message);
            logData.AppendLine(message);
        }

        public void Error(string s) {
            ShouldSave = true;
            string message = string.Format("{0} Error {1}", DateTime.Now.ToString("HH:mm:ss.fff"), s);
            System.Diagnostics.Debug.WriteLine(message);
            logData.AppendLine(message);
        }

        public void Save() {
            using (StreamWriter outfile = new StreamWriter(Path.Combine(AssemblyLoadDirectory, string.Format("Log_{0}.txt", DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"))))) {
                outfile.Write(logData);
            }
        }

        public void ConditionalSave() {
            if (ShouldSave) {
                Save();
            }
        }

        public void Save(string path) {
            using (StreamWriter outfile = new StreamWriter(path)) {
                outfile.Write(logData);
            }
        }
    }
}
