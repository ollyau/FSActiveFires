using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Linq;

namespace FSActiveFires {
    class Program {
        [STAThreadAttribute]
        public static void Main() {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
#if !DEBUG
            try {
#endif
                var args = Environment.GetCommandLineArgs();
                if (args.Length > 1) {
                    string[] logParams = { "log", "-log", "/log", "l", "-l", "/l" };
                    if (logParams.Contains(args[1], StringComparer.InvariantCultureIgnoreCase)) {
                        Log.Instance.ShouldSave = true;
                    }
                }
                App.Main();
#if !DEBUG
            }
            catch (Exception ex) {
                Log log = Log.Instance;
                log.Error(string.Format("Type: {0}\r\nMessage: {1}\r\nStack trace:\r\n{2}", ex.GetType(), ex.Message, ex.StackTrace));
                log.Save();
                throw;
            }
#endif
        }

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args) {
            Assembly executingAssembly = Assembly.GetExecutingAssembly();
            AssemblyName assemblyName = new AssemblyName(args.Name);

            string path = assemblyName.Name + ".dll";
            if (assemblyName.CultureInfo != null && assemblyName.CultureInfo.Equals(CultureInfo.InvariantCulture) == false) {
                path = String.Format(@"{0}\{1}", assemblyName.CultureInfo, path);
            }

            using (Stream stream = executingAssembly.GetManifestResourceStream(path)) {
                if (stream == null)
                    return null;

                byte[] assemblyRawBytes = new byte[stream.Length];
                stream.Read(assemblyRawBytes, 0, assemblyRawBytes.Length);
                return Assembly.Load(assemblyRawBytes);
            }
        }
    }
}
