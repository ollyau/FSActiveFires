using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FSActiveFires {
    /// <summary>
    /// Ini parsing library originally written by Steven Frost
    /// </summary>
    public class Ini : IDisposable {
        public ArrayList Locations;

        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string value, string filePath);
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string defValue, StringBuilder retVal, int size, string filePath);
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, int key, string value, [MarshalAs(UnmanagedType.LPArray)] byte[] result, int size, string filePath);
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(int section, string key, string value, [MarshalAs(UnmanagedType.LPArray)] byte[] result, int size, string filePath);
        [DllImport("kernel32.dll")]
        private static extern bool WritePrivateProfileSection(string section, string value, string filePath);

        /// <summary>
        /// Class constructor
        /// </summary>
        /// <param name="location">The location of an ini file to work with</param>
        public Ini(string location) {
            Locations = new ArrayList();
            Locations.Add(location);
        }

        /// <summary>
        /// Class constructor
        /// </summary>
        /// <param name="locations">Ini file locations to work with</param>
        public Ini(params string[] locations) {
            Locations = new ArrayList();

            foreach (string path in locations)
                Locations.Add(path);
        }

        /// <summary>
        /// Comments out the specified key in the given ini file
        /// </summary>
        /// <param name="fileID">The ID of the file to read from</param>
        /// <param name="section">The section containing the key</param>
        /// <param name="key">The key to comment out</param>
        /// <param name="commentDefinition">The comment character(s) (; by default)</param>
        public void AddKeyComment(int fileID, string section, string key, string commentDefinition = ";") {
            string value = GetKeyValue(fileID, section, key);

            RemoveKey(fileID, section, key);
            WriteKeyValue(fileID, section, String.Format("{0} {1}", commentDefinition, key), value);
            value = null;
        }

        /// <summary>
        /// Gets the names of all categories in the specified ini file
        /// </summary>
        /// <param name="fileID">The ID of the file to read from</param>
        /// <returns>String array containing all section names</returns>
        public string[] GetCategoryNames(int fileID) {
            for (int maxsize = 500; true; maxsize *= 2) {
                // Get the category names
                byte[] bytes = new byte[maxsize];
                int size = GetPrivateProfileString(0, "", "", bytes, maxsize, Locations[fileID].ToString());

                if (size < maxsize - 2) {
                    // Format the strings and return the array
                    string Selected = Encoding.ASCII.GetString(bytes, 0, size - (size > 0 ? 1 : 0));
                    return Selected.Split(new char[] { '\0' });
                }
            }
        }

        /// <summary>
        /// Gets the names of all keys in a specified section in the given ini file
        /// </summary>
        /// <param name="fileID">The ID of the file to read from</param>
        /// <param name="section">The section to real from</param>
        /// <returns>String array containing all key names in the given section</returns>
        public string[] GetKeyNames(int fileID, string section) {
            for (int maxsize = 500; true; maxsize *= 2) {
                // Get the key names
                byte[] bytes = new byte[maxsize];
                int size = GetPrivateProfileString(section, 0, "", bytes, maxsize, Locations[fileID].ToString());

                if (size < maxsize - 2) {
                    // Format the strings and return the array
                    string entries = Encoding.ASCII.GetString(bytes, 0, size - (size > 0 ? 1 : 0));
                    return entries.Split(new char[] { '\0' });
                }
            }
        }

        /// <summary>
        /// Gets the current value of the specified key in the given category
        /// </summary>
        /// <param name="fileID">The ID of the file to read from</param>
        /// <param name="section">The section in the ini file to read from</param>
        /// <param name="Key">The key in the ini file to read from</param>
        /// <returns>The current value set in the specified key</returns>
        public string GetKeyValue(int fileID, string section, string key) {
            StringBuilder stringBuilder = new StringBuilder(255);
            GetPrivateProfileString(section, key, "", stringBuilder, 255, Locations[fileID].ToString());
            return stringBuilder.ToString();
        }

        /// <summary>
        /// Determines if a value exists for a given key
        /// </summary>
        /// <param name="fileID">The ID of the file to read from</param>
        /// <param name="section">The section in the ini file to read from</param>
        /// <param name="key">The key in the ini file to read from</param>
        /// <returns>True if the value exists for the given key</returns>
        public bool KeyValueExists(int fileID, string section, string key) {
            bool valueExists = false;

            if (GetKeyValue(fileID, section, key) != "")
                valueExists = true;

            return valueExists;
        }

        /// <summary>
        /// Removes all keys in the specified section in the given ini file
        /// </summary>
        /// <param name="fileID">The ID of the file to read from</param>
        /// <param name="section">The section to remove all keys from</param>
        public void RemoveAllKeys(int fileID, string section) {
            WritePrivateProfileSection(section, "", Locations[fileID].ToString());
        }

        /// <summary>
        /// Removes a specified key from the given section in the specified ini file
        /// </summary>
        /// <param name="fileID">The ID of the file to read from</param>
        /// <param name="section">The section containing the key to be removed</param>
        /// <param name="key">The key to be removed from the section</param>
        public void RemoveKey(int fileID, string section, string key) {
            WritePrivateProfileString(section, key, null, Locations[fileID].ToString());
        }

        /// <summary>
        /// Removes the specified section from the given ini file
        /// </summary>
        /// <param name="fileID">The ID of the file to read from</param>
        /// <param name="section">The section to remove</param>
        public void RemoveSection(int fileID, string section) {
            WritePrivateProfileSection(section, null, Locations[fileID].ToString());
        }

        /// <summary>
        /// Writes a value to the specified key in a specified category in the ini file
        /// </summary>
        /// <param name="fileID">The ID of the file to write to</param>
        /// <param name="category">The section of the ini file to write to</param>
        /// <param name="key">The key to write to</param>
        /// <param name="value">The value to assign to the key</param>
        public void WriteKeyValue(int fileID, string category, string key, string value) {
            WritePrivateProfileString(category, key, value, Locations[fileID].ToString());
        }

        /// <summary>
        /// Disposes of all objects
        /// </summary>
        void IDisposable.Dispose() {
            Locations = null;
        }
    }
}
