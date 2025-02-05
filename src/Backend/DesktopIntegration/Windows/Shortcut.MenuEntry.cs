﻿/*
 * Copyright 2010-2014 Bastian Eicher
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser Public License for more details.
 * 
 * You should have received a copy of the GNU Lesser Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using Microsoft.Win32;
using NanoByte.Common.Tasks;
using ZeroInstall.DesktopIntegration.AccessPoints;
using ZeroInstall.DesktopIntegration.Properties;

namespace ZeroInstall.DesktopIntegration.Windows
{
    public static partial class Shortcut
    {
        /// <summary>
        /// Creates a new Windows shortcut in the start menu or on the start page.
        /// </summary>
        /// <param name="menuEntry">Information about the shortcut to be created.</param>
        /// <param name="target">The target the shortcut shall point to.</param>
        /// <param name="handler">A callback object used when the the user is to be informed about the progress of long-running operations such as downloads.</param>
        /// <param name="machineWide">Create the shortcut machine-wide instead of just for the current user.</param>
        public static void Create(MenuEntry menuEntry, InterfaceFeed target, ITaskHandler handler, bool machineWide = false)
        {
            #region Sanity checks
            if (menuEntry == null) throw new ArgumentNullException("menuEntry");
            if (handler == null) throw new ArgumentNullException("handler");
            #endregion

            string dirPath = GetStartMenuCategoryPath(menuEntry.Category, machineWide);
            if (!Directory.Exists(dirPath)) Directory.CreateDirectory(dirPath);

            string filePath = GetStartMenuPath(menuEntry.Category, menuEntry.Name, machineWide);
            Create(filePath, target, menuEntry.Command, handler, machineWide);
        }

        /// <summary>
        /// Removes a Windows shortcut from the start menu or on the start page.
        /// </summary>
        /// <param name="menuEntry">Information about the shortcut to be removed.</param>
        /// <param name="machineWide">The shortcut was created machine-wide instead of just for the current user.</param>
        public static void Remove(MenuEntry menuEntry, bool machineWide = false)
        {
            #region Sanity checks
            if (menuEntry == null) throw new ArgumentNullException("menuEntry");
            #endregion

            string filePath = GetStartMenuPath(menuEntry.Category, menuEntry.Name, machineWide);
            if (File.Exists(filePath)) File.Delete(filePath);

            // Delete category directory if empty
            string dirPath = GetStartMenuCategoryPath(menuEntry.Category, machineWide);
            if (Directory.Exists(dirPath) && Directory.GetFileSystemEntries(dirPath).Length == 0)
                Directory.Delete(dirPath, recursive: false);
        }

        private static string GetStartMenuCategoryPath(string category, bool machineWide)
        {
            string menuDir = machineWide
                ? Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders", "Common Programs", "").ToString()
                : Environment.GetFolderPath(Environment.SpecialFolder.Programs);
            return (String.IsNullOrEmpty(category) ? menuDir : Path.Combine(menuDir, category));
        }

        private static string GetStartMenuPath(string category, string name, bool machineWide)
        {
            if (String.IsNullOrEmpty(name) || name.IndexOfAny(Path.GetInvalidFileNameChars()) != -1)
                throw new IOException(String.Format(Resources.NameInvalidChars, name));

            return Path.Combine(GetStartMenuCategoryPath(category, machineWide), name + ".lnk");
        }
    }
}
