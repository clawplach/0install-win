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
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using ICSharpCode.SharpZipLib.Zip;
using NanoByte.Common.Collections;
using NanoByte.Common.Storage;
using NanoByte.Common.Utils;
using ZeroInstall.DesktopIntegration.Properties;
using ZeroInstall.Store.Model;

namespace ZeroInstall.DesktopIntegration
{
    /// <summary>
    /// Stores a list of applications and the kind of desktop integration the user chose for them.
    /// </summary>
    [XmlRoot("app-list", Namespace = XmlNamespace), XmlType("app-list", Namespace = XmlNamespace)]
    [XmlNamespace("xsi", XmlStorage.XsiNamespace)]
    //[XmlNamespace("caps", CapabilityList.XmlNamespace)]
    //[XmlNamespace("feed", Feed.XmlNamespace)]
    public sealed class AppList : XmlUnknown, ICloneable, IEquatable<AppList>
    {
        #region Constants
        /// <summary>
        /// The XML namespace used for storing application list data.
        /// </summary>
        public const string XmlNamespace = "http://0install.de/schema/desktop-integration/app-list";

        /// <summary>
        /// The URI to retrieve an XSD containing the XML Schema information for this class in serialized form.
        /// </summary>
        public const string XsdLocation = XmlNamespace + "/app-list.xsd";

        /// <summary>
        /// Provides XML Editors with location hints for XSD files.
        /// </summary>
        [XmlAttribute("schemaLocation", Namespace = XmlStorage.XsiNamespace)]
        public string XsiSchemaLocation = XmlNamespace + " " + XsdLocation;
        #endregion

        #region Properties
        private readonly List<AppEntry> _entries = new List<AppEntry>();

        /// <summary>
        /// A list of <see cref="AppEntry"/>s.
        /// </summary>
        [Description("A list of application entries.")]
        [XmlElement("app")]
        public List<AppEntry> Entries { get { return _entries; } }
        #endregion

        //--------------------//

        #region Access
        /// <summary>
        /// Checks whether an <see cref="AppEntry"/> for a specific interface ID exists.
        /// </summary>
        /// <param name="interfaceID">The <see cref="AppEntry.InterfaceID"/> to look for.</param>
        /// <returns><see langword="true"/> if a matching entry was found; <see langword="false"/> otherwise.</returns>
        public bool ContainsEntry(string interfaceID)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(interfaceID)) throw new ArgumentNullException("interfaceID");
            #endregion

            return Entries.Any(entry => ModelUtils.IDEquals(entry.InterfaceID, interfaceID));
        }

        /// <summary>
        /// Gets an <see cref="AppEntry"/> for a specific interface ID.
        /// </summary>
        /// <param name="interfaceID">The <see cref="AppEntry.InterfaceID"/> to look for.</param>
        /// <returns>The first matching <see cref="AppEntry"/>.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if no entry matching the interface ID was found.</exception>
        public AppEntry this[string interfaceID]
        {
            get
            {
                #region Sanity checks
                if (string.IsNullOrEmpty(interfaceID)) throw new ArgumentNullException("interfaceID");
                #endregion

                try
                {
                    return Entries.First(entry => ModelUtils.IDEquals(entry.InterfaceID, interfaceID));
                }
                    #region Error handling
                catch (InvalidOperationException)
                {
                    throw new KeyNotFoundException(string.Format(Resources.AppNotInList, interfaceID));
                }
                #endregion
            }
        }

        /// <summary>
        /// Gets an <see cref="AppEntry"/> for a specific interface ID. Safe for missing elements.
        /// </summary>
        /// <param name="interfaceID">The <see cref="AppEntry.InterfaceID"/> to look for.</param>
        /// <returns>The first matching <see cref="AppEntry"/>; <see langword="null"/> if no match was found.</returns>
        public AppEntry GetEntry(string interfaceID)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(interfaceID)) throw new ArgumentNullException("interfaceID");
            #endregion

            return Entries.FirstOrDefault(entry => ModelUtils.IDEquals(entry.InterfaceID, interfaceID));
        }
        #endregion

        #region Storage
        /// <summary>
        /// Returns the default file path used to store the main <see cref="AppList"/> on this system.
        /// </summary>
        /// <param name="machineWide">Store the <see cref="AppList"/> machine-wide instead of just for the current user.</param>
        public static string GetDefaultPath(bool machineWide = false)
        {
            return Path.Combine(
                // Machine-wide storage cannot be portable, per-user storage can be portable
                machineWide ? Locations.GetIntegrationDirPath("0install.net", true, "desktop-integration") : Locations.GetSaveConfigPath("0install.net", false, "desktop-integration"),
                "app-list.xml");
        }

        /// <summary>
        /// Loads a list from an XML file embedded in a ZIP archive.
        /// </summary>
        /// <param name="stream">The ZIP archive to load.</param>
        /// <param name="password">The password to use for decryption; <see langword="null"/> for no encryption.</param>
        /// <returns>The loaded list.</returns>
        /// <exception cref="ZipException">Thrown if a problem occurred while reading the ZIP data or if <paramref name="password"/> is wrong.</exception>
        /// <exception cref="InvalidDataException">Thrown if a problem occurred while deserializing the XML data.</exception>
        public static AppList LoadXmlZip(Stream stream, string password = null)
        {
            #region Sanity checks
            if (stream == null) throw new ArgumentNullException("stream");
            #endregion

            using (var zipFile = new ZipFile(stream) {IsStreamOwner = false, Password = password})
            {
                var zipEntry = zipFile.Cast<ZipEntry>().First(x => StringUtils.EqualsIgnoreCase(x.Name, "data.xml"));

                try
                {
                    return XmlStorage.LoadXml<AppList>(zipFile.GetInputStream(zipEntry));
                }
                catch (InvalidOperationException)
                {
                    throw new InvalidDataException(Resources.SyncServerDataDamaged);
                }
            }
        }

        /// <summary>
        /// Saves the list in an XML file embedded in a ZIP archive.
        /// </summary>
        /// <param name="stream">The ZIP archive to be written.</param>
        /// <param name="password">The password to use for encryption; <see langword="null"/> for no encryption.</param>
        public void SaveXmlZip(Stream stream, string password = null)
        {
            #region Sanity checks
            if (stream == null) throw new ArgumentNullException("stream");
            #endregion

            if (stream.CanSeek) stream.Position = 0;
            using (var zipStream = new ZipOutputStream(stream) {IsStreamOwner = false})
            {
                if (!string.IsNullOrEmpty(password)) zipStream.Password = password;

                // Write the XML file to the ZIP archive
                {
                    var entry = new ZipEntry("data.xml") {DateTime = DateTime.Now};
                    if (!string.IsNullOrEmpty(password)) entry.AESKeySize = 128;
                    zipStream.SetLevel(9);
                    zipStream.PutNextEntry(entry);
                    this.SaveXml(zipStream);
                    zipStream.CloseEntry();
                }
            }
        }
        #endregion

        //--------------------//

        #region Clone
        /// <summary>
        /// Creates a deep copy of this <see cref="AppList"/> instance.
        /// </summary>
        /// <returns>The new copy of the <see cref="AppList"/>.</returns>
        public AppList Clone()
        {
            var appList = new AppList {UnknownAttributes = UnknownAttributes, UnknownElements = UnknownElements};
            foreach (var entry in Entries) appList.Entries.Add(entry.Clone());

            return appList;
        }

        object ICloneable.Clone()
        {
            return Clone();
        }
        #endregion

        #region Equality
        /// <inheritdoc/>
        public bool Equals(AppList other)
        {
            if (other == null) return false;
            return base.Equals(other) && Entries.UnsequencedEquals(other.Entries);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is AppList && Equals((AppList)obj);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode() * 397) ^ Entries.GetUnsequencedHashCode();
            }
        }
        #endregion
    }
}
