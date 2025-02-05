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
using System.ComponentModel;
using System.Xml.Serialization;
using ZeroInstall.Store.Model.Design;

namespace ZeroInstall.Store.Model
{
    /// <summary>
    /// Zero Install will not know how to run a program using generic bindings itself, but it will include them in any selections documents it creates, which can then be executed by your custom code.
    /// </summary>
    [Description("Zero Install will not know how to run a program using generic bindings itself, but it will include them in any selections documents it creates, which can then be executed by your custom code.")]
    [Serializable]
    [XmlRoot("binding", Namespace = Feed.XmlNamespace), XmlType("binding", Namespace = Feed.XmlNamespace)]
    public sealed class GenericBinding : Binding, IEquatable<GenericBinding>
    {
        #region Properties
        /// <summary>
        /// If your binding needs a path within the selected implemention, it is suggested that the path attribute be used for this. Other attributes and child elements should be namespaced to avoid collisions. 
        /// </summary>
        [Description("If your binding needs a path within the selected implemention, it is suggested that the path attribute be used for this. Other attributes and child elements should be namespaced to avoid collisions. ")]
        [XmlAttribute("path")]
        public string Path { get; set; }

        /// <summary>
        /// If set Install will select the given command within the implementation (which may cause additional dependencies and bindings to be selected). Otherwise, no command is selected.
        /// </summary>
        [Description("If set Zero Install will select the given command within the implementation (which may cause additional dependencies and bindings to be selected). Otherwise, no command is selected.")]
        [XmlAttribute("command"), DefaultValue("")]
        [TypeConverter(typeof(CommandNameConverter))]
        public string Command { get; set; }
        #endregion

        //--------------------//

        #region Conversion
        /// <summary>
        /// Returns the binding in the form "Path = Command". Not safe for parsing!
        /// </summary>
        public override string ToString()
        {
            return string.Format("{0} = {1}", Path, Command);
        }
        #endregion

        #region Clone
        /// <summary>
        /// Creates a deep copy of this <see cref="GenericBinding"/> instance.
        /// </summary>
        /// <returns>The new copy of the <see cref="GenericBinding"/>.</returns>
        public override Binding Clone()
        {
            return new GenericBinding {UnknownAttributes = UnknownAttributes, UnknownElements = UnknownElements, IfZeroInstallVersion = IfZeroInstallVersion, Path = Path, Command = Command};
        }
        #endregion

        #region Equality
        /// <inheritdoc/>
        public bool Equals(GenericBinding other)
        {
            if (other == null) return false;
            return base.Equals(other) && other.Path == Path || other.Command == Command;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is GenericBinding && Equals((GenericBinding)obj);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                int result = base.GetHashCode();
                result = (result * 397) ^ (Path ?? "").GetHashCode();
                result = (result * 397) ^ (Command ?? "").GetHashCode();
                return result;
            }
        }
        #endregion
    }
}
