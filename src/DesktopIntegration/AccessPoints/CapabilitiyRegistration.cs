﻿/*
 * Copyright 2010-2011 Bastian Eicher
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
using System.Xml.Serialization;
using Common.Collections;
using Common.Tasks;
using ZeroInstall.Model;
using Capabilities = ZeroInstall.Model.Capabilities;

namespace ZeroInstall.DesktopIntegration.AccessPoints
{
    /// <summary>
    /// Indicates that all compatible capabilities should be registered.
    /// </summary>
    /// <seealso cref="ZeroInstall.Model.Capabilities"/>
    [XmlType("capability-registration", Namespace = AppList.XmlNamespace)]
    public class CapabilityRegistration : AccessPoint, IEquatable<CapabilityRegistration>
    {
        #region Constants
        /// <summary>
        /// The name of this category of <see cref="AccessPoint"/>s as used by command-line interfaces.
        /// </summary>
        public const string CategoryName = "capabilities";
        #endregion

        //--------------------//

        #region Collision
        /// <inheritdoc/>
        public override IEnumerable<string> GetConflictIDs(AppEntry appEntry)
        {
            #region Sanity checks
            if (appEntry == null) throw new ArgumentNullException("appEntry");
            #endregion

            var idList = new LinkedList<string>();
            foreach (var capabilityList in appEntry.CapabilityLists.FindAll(list => list.Architecture.IsCompatible(Architecture.CurrentSystem)))
            {
                foreach (var capability in capabilityList.Entries)
                    idList.AddLast("capability:" + capability.ConflictIDs);
            }
            return idList;
        }
        #endregion

        #region Apply
        /// <inheritdoc/>
        public override void Apply(AppEntry appEntry, InterfaceFeed target, bool systemWide, ITaskHandler handler)
        {
            #region Sanity checks
            if (appEntry == null) throw new ArgumentNullException("appEntry");
            #endregion

            foreach (var capabilityList in appEntry.CapabilityLists.FindAll(list => list.Architecture.IsCompatible(Architecture.CurrentSystem)))
            {
                foreach (var fileType in EnumerableUtils.OfType<Capabilities.FileType>(capabilityList.Entries))
                    Windows.FileType.Apply(target, fileType, false, systemWide, handler);
            }

            // ToDo: Uninstall entry
        }

        /// <inheritdoc/>
        public override void Unapply(AppEntry appEntry, bool systemWide)
        {
            // ToDo: Implement
        }
        #endregion

        //--------------------//

        #region Conversion
        /// <summary>
        /// Returns the access point in the form "CapabilityRegistration". Not safe for parsing!
        /// </summary>
        public override string ToString()
        {
            return string.Format("CapabilityRegistration");
        }
        #endregion

        #region Clone
        /// <inheritdoc/>
        public override AccessPoint CloneAccessPoint()
        {
            return new CapabilityRegistration();
        }
        #endregion

        #region Equality
        /// <inheritdoc/>
        public bool Equals(CapabilityRegistration other)
        {
            if (other == null) return false;

            return true;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == typeof(CapabilityRegistration) && Equals((CapabilityRegistration)obj);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return 0;
        }
        #endregion
    }
}
