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
using System.Xml.Serialization;
using NanoByte.Common.Tasks;
using NanoByte.Common.Utils;
using ZeroInstall.Store.Model;

namespace ZeroInstall.DesktopIntegration.AccessPoints
{
    /// <summary>
    /// Makes an application discoverable via the system's search PATH.
    /// </summary>
    [XmlType("alias", Namespace = AppList.XmlNamespace)]
    public class AppAlias : CommandAccessPoint, IEquatable<AppAlias>
    {
        #region Constants
        /// <summary>
        /// The name of this category of <see cref="AccessPoint"/>s as used by command-line interfaces.
        /// </summary>
        public const string CategoryName = "aliases";
        #endregion

        #region Properties
        /// <summary>
        /// The name of the command-line command (without a file extension).
        /// </summary>
        [Description("The name of the command-line command (without a file extension).")]
        [XmlAttribute("name")]
        public string Name { get; set; }
        #endregion

        //--------------------//

        #region Conflict ID
        /// <inheritdoc/>
        public override IEnumerable<string> GetConflictIDs(AppEntry appEntry)
        {
            return new[] {"alias:" + Name};
        }
        #endregion

        #region Apply
        /// <inheritdoc/>
        public override void Apply(AppEntry appEntry, Feed feed, ITaskHandler handler, bool machineWide)
        {
            #region Sanity checks
            if (appEntry == null) throw new ArgumentNullException("appEntry");
            if (handler == null) throw new ArgumentNullException("handler");
            #endregion

            var target = new InterfaceFeed(appEntry.InterfaceID, feed);
            if (WindowsUtils.IsWindows) Windows.AppAlias.Create(target, Command, Name, machineWide, handler);
            else if (UnixUtils.IsUnix) Unix.AppAlias.Create(target, Command, Name, handler, machineWide);
        }

        /// <inheritdoc/>
        public override void Unapply(AppEntry appEntry, bool machineWide)
        {
            #region Sanity checks
            if (appEntry == null) throw new ArgumentNullException("appEntry");
            #endregion

            if (WindowsUtils.IsWindows) Windows.AppAlias.Remove(Name, machineWide);
            else if (UnixUtils.IsUnix) Unix.AppAlias.Remove(Name, machineWide);
        }
        #endregion

        //--------------------//

        #region Conversion
        /// <summary>
        /// Returns the access point in the form "AppAlias: Name (Command)". Not safe for parsing!
        /// </summary>
        public override string ToString()
        {
            return string.Format("AppAlias: {0} ({1})", Name, Command);
        }
        #endregion

        #region Clone
        /// <inheritdoc/>
        public override AccessPoint Clone()
        {
            return new AppAlias {UnknownAttributes = UnknownAttributes, UnknownElements = UnknownElements, Command = Command, Name = Name};
        }
        #endregion

        #region Equality
        /// <inheritdoc/>
        public bool Equals(AppAlias other)
        {
            if (other == null) return false;
            return base.Equals(other) && other.Name == Name;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == typeof(AppAlias) && Equals((AppAlias)obj);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                int result = base.GetHashCode();
                result = (result * 397) ^ (Name ?? "").GetHashCode();
                return result;
            }
        }
        #endregion
    }
}
