/****************************** Module Header ******************************\
Module Name:  ini.cs
Project:      INI File Parser
Copyright (c) Steven Frost.

This file implements a simple INI file parser which allows manipulation of
standard-format INI files.

This source is subject to the MIT License.
See http://opensource.org/licenses/MIT

All other rights reserved.
THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND,
EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
\***************************************************************************/

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace iniLib {
    public class Ini : IDisposable {
        public string path;

        [DllImport("kernel32.dll")]
        private static extern bool WritePrivateProfileString(string section, string key, string value, string filePath);
        [DllImport("kernel32.dll")]
        private static extern uint GetPrivateProfileString(string section, string key, string defValue, StringBuilder retVal, uint size, string filePath);
        [DllImport("kernel32.dll")]
        private static extern uint GetPrivateProfileString(string section, int key, string value, [MarshalAs(UnmanagedType.LPArray)] byte[] result, uint size, string filePath);
        [DllImport("kernel32.dll")]
        private static extern uint GetPrivateProfileString(int section, string key, string value, [MarshalAs(UnmanagedType.LPArray)] byte[] result, uint size, string filePath);
        [DllImport("kernel32.dll")]
        private static extern bool WritePrivateProfileSection(string section, string value, string filePath);

        /// <summary>
        /// Class constructor
        /// </summary>
        /// <param name="path">The location of an ini file to work with</param>
        public Ini(string path) {
            this.path = path;
        }

        /// <summary>
        /// Comments out the specified key
        /// </summary>
        /// <param name="section">The section containing the key</param>
        /// <param name="key">The key to comment out</param>
        /// <param name="commentDefinition">The comment character(s) (; by default)</param>
        public void AddKeyComment(string section, string key, string commentDefinition = ";") {
            string value = GetKeyValue(section, key);
            RemoveKey(section, key);
            WriteKeyValue(section, String.Format("{0} {1}", commentDefinition, key), value);
        }

        /// <summary>
        /// Gets the names of all categories in the ini file
        /// </summary>
        /// <returns>Array of category names</returns>
        public string[] GetCategoryNames() {
            for (uint maxsize = 500; true; maxsize *= 2) {
                byte[] bytes = new byte[maxsize];
                int size = (int)GetPrivateProfileString(0, "", "", bytes, maxsize, path);

                if (size < (int)(maxsize - 2)) {
                    string selected = Encoding.ASCII.GetString(bytes, 0, size - (size > 0 ? 1 : 0));
                    return selected.Split(new char[] { '\0' });
                }
            }
        }

        /// <summary>
        /// Gets the names of all keys in a specified section in the ini file
        /// </summary>
        /// <param name="section">The section to read from</param>
        /// <returns>Array of key names</returns>
        public string[] GetKeyNames(string section) {
            for (uint maxsize = 500; true; maxsize *= 2) {
                byte[] bytes = new byte[maxsize];
                int size = (int)GetPrivateProfileString(section, 0, "", bytes, maxsize, path);

                if (size < (int)(maxsize - 2)) {
                    string entries = Encoding.ASCII.GetString(bytes, 0, size - (size > 0 ? 1 : 0));
                    return entries.Split(new char[] { '\0' });
                }
            }
        }

        /// <summary>
        /// Gets the current value of a key in a category
        /// </summary>
        /// <param name="section">The section in the ini file to read from</param>
        /// <param name="Key">The key in the ini file to read from</param>
        /// <returns>The key value</returns>
        public string GetKeyValue(string section, string key) {
            StringBuilder stringBuilder = new StringBuilder(255);
            GetPrivateProfileString(section, key, "", stringBuilder, 255, path);
            return stringBuilder.ToString();
        }

        /// <summary>
        /// Determines if a value exists for a key
        /// </summary>
        /// <param name="section">The section the key is contained in</param>
        /// <param name="key">The key to check</param>
        /// <returns>True if the value exists for the key</returns>
        public bool KeyValueExists(string section, string key) {
            return (GetKeyValue(section, key) != "");
        }

        /// <summary>
        /// Removes all keys in the specified section
        /// </summary>
        /// <param name="section">The section to remove all keys from</param>
        public void RemoveAllKeys(string section) {
            WritePrivateProfileSection(section, "", path);
        }

        /// <summary>
        /// Removes a specified key from a section in the ini file
        /// </summary>
        /// <param name="section">The section containing the key to be removed</param>
        /// <param name="key">The key to be removed</param>
        public void RemoveKey(string section, string key) {
            WritePrivateProfileString(section, key, null, path);
        }

        /// <summary>
        /// Removes a section from the ini file
        /// </summary>
        /// <param name="section">The section to remove</param>
        public void RemoveSection(string section) {
            WritePrivateProfileSection(section, null, path);
        }

        /// <summary>
        /// Writes a value to a key in a specific category
        /// </summary>
        /// <param name="category">The section containing the key</param>
        /// <param name="key">The key to write</param>
        /// <param name="value">The value to write</param>
        public void WriteKeyValue(string category, string key, string value) {
            WritePrivateProfileString(category, key, value, path);
        }

        /// <summary>
        /// Disposes of all objects
        /// </summary>
        void IDisposable.Dispose() {
            path = null;
        }
    }
}