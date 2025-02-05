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
using System.Text;
using NanoByte.Common.Info;
using NanoByte.Common.Storage;
using ZeroInstall.Commands.Properties;

namespace ZeroInstall.Commands
{
    /// <summary>
    /// The default command used when no command is explicitly specified.
    /// </summary>
    [CLSCompliant(false)]
    public sealed class DefaultCommand : FrontendCommand
    {
        #region Metadata
        /// <inheritdoc/>
        protected override string Description
        {
            get
            {
                var builder = new StringBuilder(Resources.TryHelpWith + Environment.NewLine);
                foreach (var possibleCommand in CommandFactory.CommandNames)
                    builder.AppendLine("0install " + possibleCommand);
                return builder.ToString();
            }
        }

        /// <inheritdoc/>
        protected override string Usage { get { return "COMMAND"; } }

        /// <inheritdoc/>
        protected override int AdditionalArgsMax { get { return 0; } }

        /// <inheritdoc/>
        public DefaultCommand(ICommandHandler handler) : base(handler)
        {
            Options.Add("V|version", () => Resources.OptionVersion, _ =>
            {
                Handler.Output(Resources.VersionInformation, AppInfo.Current.Name + " " + AppInfo.Current.Version + (Locations.IsPortable ? " - " + Resources.PortableMode : "") + Environment.NewLine + AppInfo.Current.Copyright + Environment.NewLine + Resources.LicenseInfo);
                throw new OperationCanceledException(); // Don't handle any of the other arguments
            });
        }
        #endregion

        /// <inheritdoc/>
        public override int Execute()
        {
            Handler.Output(Resources.CommandLineArguments, HelpText);
            return 1;
        }
    }
}
