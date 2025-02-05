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
using System.Linq;
using System.Xml.Serialization;
using NanoByte.Common.Collections;
using NanoByte.Common.Dispatch;
using NanoByte.Common.Utils;
using ZeroInstall.DesktopIntegration.AccessPoints;
using ZeroInstall.DesktopIntegration.Properties;
using ZeroInstall.Store;
using ZeroInstall.Store.Model;
using ZeroInstall.Store.Model.Capabilities;

namespace ZeroInstall.DesktopIntegration
{
    /// <summary>
    /// Represents an application in the <see cref="AppList"/> indentified by its interface URI.
    /// </summary>
    [XmlType("app", Namespace = AppList.XmlNamespace)]
    public sealed class AppEntry : XmlUnknown, IMergeable<AppEntry>, ICloneable
    {
        #region Properties
        /// <summary>
        /// The URI or local path of the interface defining the application or the pet-name if <see cref="Requirements"/> is set.
        /// </summary>
        [Description("The URI or local path of the interface defining the application or the pet-name if Requirements is set.")]
        [XmlAttribute("interface")]
        public string InterfaceID { get; set; }

        string IMergeable<AppEntry>.MergeID { get { return InterfaceID; } }

        /// <summary>
        /// The name of the application. Usually equal to <see cref="Feed.Name"/>.
        /// </summary>
        [Description("The name of the application. Usually equal to the Name specified in the Feed.")]
        [XmlAttribute("name")]
        public string Name { get; set; }

        private bool _autoUpdate = true;

        /// <summary>
        /// Set to <see langword="true"/> to automatically download the newest available version of the application as a regular background task. Update checks will still be performed when the application is launched when set to <see langword="false"/>.
        /// </summary>
        [Description("Set to true to automatically download the newest available version of the application as a regular background task. Update checks will still be performed when the application is launched when set to false.")]
        [XmlAttribute("auto-update"), DefaultValue(true)]
        public bool AutoUpdate { get { return _autoUpdate; } set { _autoUpdate = value; } }

        /// <summary>
        /// A regular expression a computer's hostname must match for this entry to be applied. Enables machine-specific entry filtering.
        /// </summary>
        [Description("A regular expression a computer's hostname must match for this entry to be applied. Enables machine-specific entry filtering.")]
        [XmlAttribute("hostname"), DefaultValue("")]
        public string Hostname { get; set; }

        /// <summary>
        /// A set of requirements/restrictions imposed by the user on the implementation selection process. May be <see langword="null"/> if <see cref="InterfaceID"/> is not a pet-name.
        /// </summary>
        [Description("A set of requirements/restrictions imposed by the user on the implementation selection process. May be null if InterfaceID is not a pet-name.")]
        [XmlIgnore]
        public Requirements Requirements { get; set; }

        /// <summary>Used for XML+JSON serialization.</summary>
        /// <seealso cref="Requirements"/>
        [XmlElement("requirements-json"), Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string RequirementsJson { get { return (Requirements == null) ? null : Requirements.ToJsonString(); } set { Requirements = JsonStorage.FromJsonString<Requirements>(value); } }

        private readonly List<CapabilityList> _capabilityLists = new List<CapabilityList>();

        /// <summary>
        /// A set of <see cref="Capability"/> lists to be registered in the desktop environment. Only compatible architectures are handled.
        /// </summary>
        [Description("A set of Capability lists to be registered in the desktop environment. Only compatible architectures are handled.")]
        [XmlElement("capabilities", Namespace = CapabilityList.XmlNamespace)]
        // Note: Can not use ICollection<T> interface with XML Serialization
        public List<CapabilityList> CapabilityLists
        {
            get { return _capabilityLists; }
        }

        /// <summary>
        /// A set of <see cref="AccessPoints"/>s to be registered in the desktop environment. Is <see langword="null"/> if no desktop integration has been performed yet.
        /// </summary>
        [Description("A set of AccessPoints to be registered in the desktop environment. Is null if no desktop integration has been performed yet.")]
        [XmlElement("access-points")]
        public AccessPointList AccessPoints { get; set; }

        /// <inheritdoc/>
        [Browsable(false)]
        [XmlIgnore]
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// The time this entry was last modified encoded as Unix time (number of seconds since the epoch).
        /// </summary>
        /// <remarks>This value is ignored by clone and equality methods.</remarks>
        [Browsable(false)]
        [XmlAttribute("timestamp"), DefaultValue(0)]
        public long TimestampUnix { get { return Timestamp.ToUnixTime(); } set { Timestamp = FileUtils.FromUnixTime(value); } }
        #endregion

        //--------------------//

        #region Access
        /// <summary>
        /// Retrieves the first <see cref="Capability"/> that matches a specific type and ID and is compatible with <see cref="Architecture.CurrentSystem"/>.
        /// </summary>
        /// <typeparam name="T">The capability type to match.</typeparam>
        /// <param name="id">The <see cref="Capability.ID"/> to match.</param>
        /// <returns>The first matching <see cref="Capability"/> or <see langword="null"/> if none was found.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if no capability matching <paramref name="id"/> and <typeparamref name="T"/> was found.</exception>
        public T GetCapability<T>(string id) where T : Capability
        {
            try
            {
                return _capabilityLists.
                    Where(capabilityList => capabilityList.Architecture.IsCompatible()).
                    SelectMany(capabilityList => capabilityList.Entries.OfType<T>().Where(specificCapability => specificCapability.ID == id)).
                    First();
            }
                #region Error handling
            catch (InvalidOperationException)
            {
                throw new KeyNotFoundException(string.Format(Resources.UnableToFindTypeID, typeof(T).Name, id));
            }
            #endregion
        }
        #endregion

        //--------------------//

        #region Conversion
        /// <summary>
        /// Returns the entry in the form "Name (InterfaceID)". Not safe for parsing!
        /// </summary>
        public override string ToString()
        {
            return string.Format("{0} ({1})", Name, InterfaceID);
        }
        #endregion

        #region Clone
        /// <summary>
        /// Creates a deep copy of this <see cref="AppEntry"/> instance.
        /// </summary>
        /// <returns>The new copy of the <see cref="AppEntry"/>.</returns>
        public AppEntry Clone()
        {
            var appList = new AppEntry {UnknownAttributes = UnknownAttributes, UnknownElements = UnknownElements, Name = Name, InterfaceID = InterfaceID};
            if (Requirements != null) appList.Requirements = Requirements.Clone();
            if (AccessPoints != null) appList.AccessPoints = AccessPoints.Clone();
            foreach (var list in CapabilityLists) appList.CapabilityLists.Add(list.Clone());

            return appList;
        }

        object ICloneable.Clone()
        {
            return Clone();
        }
        #endregion

        #region Equality
        /// <inheritdoc/>
        public bool Equals(AppEntry other)
        {
            if (other == null) return false;
            if (!base.Equals(other)) return false;
            if (!ModelUtils.IDEquals(InterfaceID, other.InterfaceID)) return false;
            if (Name != other.Name) return false;
            if (AutoUpdate != other.AutoUpdate) return false;
            if (!Equals(Requirements, other.Requirements)) return false;
            if (!CapabilityLists.SequencedEquals(other.CapabilityLists)) return false;
            if (!Equals(AccessPoints, other.AccessPoints)) return false;
            return true;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is AppEntry && Equals((AppEntry)obj);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                int result = base.GetHashCode();
                // Use case-insensitive hashing in case a business logic rule causes case-insensitive comparison of IDs)
                if (InterfaceID != null) result = (result * 397) ^ StringComparer.OrdinalIgnoreCase.GetHashCode(InterfaceID);
                if (Name != null) result = (result * 397) ^ Name.GetHashCode();
                result = (result * 397) ^ AutoUpdate.GetHashCode();
                if (Requirements != null) result = (result * 397) ^ Requirements.GetHashCode();
                result = (result * 397) ^ CapabilityLists.GetSequencedHashCode();
                if (AccessPoints != null) result = (result * 397) ^ AccessPoints.GetHashCode();
                return result;
            }
        }
        #endregion
    }
}
