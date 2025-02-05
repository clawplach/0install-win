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
using NanoByte.Common.Utils;
using ZeroInstall.Commands.Properties;

namespace ZeroInstall.Commands
{
    /// <summary>
    /// Opens the central graphical user interface for launching and managing applications.
    /// </summary>
    [CLSCompliant(false)]
    public sealed class Central : FrontendCommand
    {
        #region Metadata
        /// <summary>The name of this command as used in command-line arguments in lower-case.</summary>
        public new const string Name = "central";

        /// <inheritdoc/>
        protected override string Description { get { return Resources.DescriptionCentral; } }

        /// <inheritdoc/>
        protected override string Usage { get { return "[OPTIONS]"; } }

        /// <inheritdoc/>
        protected override int AdditionalArgsMax { get { return 0; } }

        /// <inheritdoc/>
        public Central(ICommandHandler handler) : base(handler)
        {
            Options.Add("m|machine", () => Resources.OptionMachine, _ => _machineWide = true);
        }
        #endregion

        #region State
        private bool _machineWide;
        #endregion

        /// <inheritdoc/>
        public override int Execute()
        {
            return ProcessUtils.RunAssembly(
                /*MonoUtils.IsUnix ? "ZeroInstall-gtk" :*/ "ZeroInstall",
                _machineWide ? "-m" : null);
        }
    }
}
