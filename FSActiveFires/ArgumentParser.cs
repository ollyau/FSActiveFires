using System;
using System.Linq;

namespace FSActiveFires {
    class ArgumentParser {
        string[] commandLineArgs;
        char[] separators = { ':', '=' };
        char[] startingMarkers = { '-', '/' };

        public ArgumentParser() {
            commandLineArgs = Environment.GetCommandLineArgs();
        }

        private string CleanName(string arg) {
            foreach (char c in startingMarkers) {
                arg.TrimStart(c);
            }

            foreach (char c in separators) {
                if (arg.Contains(c)) {
                    arg = arg.Remove(arg.IndexOf(c));
                }
            }

            return arg;
        }

        public void Check(string argName, Action func) {
            argName = CleanName(argName);
            foreach (char start in startingMarkers) {
                if (commandLineArgs.Any(x => x.Equals(start + argName, StringComparison.InvariantCultureIgnoreCase))) {
                    func();
                    return;
                }
            }
        }

        public void Check(string argName, Action<string> func) {
            argName = CleanName(argName);
            foreach (char start in startingMarkers) {
                int lhsLength = argName.Length + 2;
                Func<string, bool> predicate = x => x.StartsWith(start + argName, StringComparison.InvariantCultureIgnoreCase) && x.Length > lhsLength;
                if (commandLineArgs.Any(predicate)) {
                    foreach (char separator in separators) {
                        string arg = commandLineArgs.Single(predicate);
                        if (arg[lhsLength - 1].Equals(separator)) {
                            string argRhs = arg.Remove(0, lhsLength);
                            func(argRhs.Replace("\"", string.Empty));
                            return;
                        }
                    }
                }
            }
        }
    }
}
