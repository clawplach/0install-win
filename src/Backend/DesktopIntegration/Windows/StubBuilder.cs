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
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using NanoByte.Common;
using NanoByte.Common.Storage;
using NanoByte.Common.Streams;
using NanoByte.Common.Tasks;
using NanoByte.Common.Utils;
using ZeroInstall.DesktopIntegration.Properties;
using ZeroInstall.Store.Icons;
using ZeroInstall.Store.Model;

namespace ZeroInstall.DesktopIntegration.Windows
{
    /// <summary>
    /// Utility class for building stub EXEs that execute "0install" commands. Provides persistent local paths.
    /// </summary>
    public static class StubBuilder
    {
        #region Get
        /// <summary>
        /// Builds a stub EXE in a well-known location. Future calls with the same arguments will return the same EXE.
        /// </summary>
        /// <param name="target">The application to be launched via the stub.</param>
        /// <param name="machineWide">Store the stub in a machine-wide directory instead of just for the current user.</param>
        /// <param name="handler">A callback object used when the the user is to be informed about the progress of long-running operations such as downloads.</param>
        /// <param name="command">The command argument to be passed to the the "0install run" command; may be <see langword="null"/>.</param>
        /// <returns>The path to the generated stub EXE.</returns>
        /// <exception cref="OperationCanceledException">Thrown if the user canceled the task.</exception>
        /// <exception cref="IOException">Thrown if a problem occurs while writing to the filesystem.</exception>
        /// <exception cref="WebException">Thrown if a problem occured while downloading additional data (such as icons).</exception>
        /// <exception cref="InvalidOperationException">Thrown if write access to the filesystem is not permitted.</exception>
        public static string GetRunStub(this InterfaceFeed target, bool machineWide, ITaskHandler handler, string command = null)
        {
            #region Sanity checks
            if (handler == null) throw new ArgumentNullException("handler");
            #endregion

            var entryPoint = target.Feed.GetEntryPoint(command ?? Command.NameRun);
            string exeName = (entryPoint != null)
                ? entryPoint.BinaryName ?? entryPoint.Command
                : ModelUtils.Escape(target.Feed.Name);
            bool needsTerminal = target.Feed.NeedsTerminal || (entryPoint != null && entryPoint.NeedsTerminal);

            string hash = (target.InterfaceID + "#" + command).Hash(SHA256.Create());
            string path = Path.Combine(Locations.GetIntegrationDirPath("0install.net", machineWide, "desktop-integration", "stubs", hash), exeName + ".exe");

            target.CreateOrUpdateRunStub(path, handler, needsTerminal, command);
            return path;
        }

        /// <summary>
        /// Creates a new or updates an existing stub EXE that executes the "0install run" command. 
        /// </summary>
        /// <seealso cref="BuildRunStub"/>
        /// <param name="target">The application to be launched via the stub.</param>
        /// <param name="path">The target path to store the generated EXE file.</param>
        /// <param name="handler">A callback object used when the the user is to be informed about the progress of long-running operations such as downloads.</param>
        /// <param name="needsTerminal"><see langword="true"/> to build a CLI stub, <see langword="false"/> to build a GUI stub.</param>
        /// <param name="command">The command argument to be passed to the the "0install run" command; may be <see langword="null"/>.</param>
        /// <exception cref="OperationCanceledException">Thrown if the user canceled the task.</exception>
        /// <exception cref="InvalidOperationException">Thrown if there was a compilation error while generating the stub EXE.</exception>
        /// <exception cref="IOException">Thrown if a problem occurs while writing to the filesystem.</exception>
        /// <exception cref="WebException">Thrown if a problem occured while downloading additional data (such as icons).</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown if write access to the filesystem is not permitted.</exception>
        private static void CreateOrUpdateRunStub(this InterfaceFeed target, string path, ITaskHandler handler, bool needsTerminal, string command)
        {
            if (File.Exists(path))
            { // Existing stub
                // TODO: Find better rebuild discriminator
                if (File.GetLastWriteTime(path) < Process.GetCurrentProcess().StartTime)
                { // Outdated, try to rebuild
                    try
                    {
                        File.Delete(path);
                    }
                        #region Error handling
                    catch (IOException ex)
                    {
                        Log.Warn(string.Format(Resources.UnableToReplaceStub, path));
                        Log.Warn(ex);
                        return;
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        Log.Warn(string.Format(Resources.UnableToReplaceStub, path));
                        Log.Warn(ex);
                        return;
                    }
                    #endregion

                    target.BuildRunStub(path, handler, needsTerminal, command);
                }
            }
            else
            { // No existing stub, build new one
                target.BuildRunStub(path, handler, needsTerminal, command);
            }
        }
        #endregion

        #region Build
        /// <summary>
        /// Builds a stub EXE that executes the "0install run" command.
        /// </summary>
        /// <param name="target">The application to be launched via the stub.</param>
        /// <param name="path">The target path to store the generated EXE file.</param>
        /// <param name="handler">A callback object used when the the user is to be informed about the progress of long-running operations such as downloads.</param>
        /// <param name="needsTerminal"><see langword="true"/> to build a CLI stub, <see langword="false"/> to build a GUI stub.</param>
        /// <param name="command">The command argument to be passed to the the "0install run" command; may be <see langword="null"/>.</param>
        /// <exception cref="OperationCanceledException">Thrown if the user canceled the task.</exception>
        /// <exception cref="InvalidOperationException">Thrown if there was a compilation error while generating the stub EXE.</exception>
        /// <exception cref="IOException">Thrown if a problem occurs while writing to the filesystem.</exception>
        /// <exception cref="WebException">Thrown if a problem occured while downloading additional data (such as icons).</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown if write access to the filesystem is not permitted.</exception>
        internal static void BuildRunStub(this InterfaceFeed target, string path, ITaskHandler handler, bool needsTerminal, string command)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException("path");
            if (handler == null) throw new ArgumentNullException("handler");
            #endregion

            var compilerParameters = new CompilerParameters
            {
                GenerateExecutable = true,
                OutputAssembly = path,
                IncludeDebugInformation = false,
                GenerateInMemory = false,
                TreatWarningsAsErrors = true,
                ReferencedAssemblies = {"System.dll"}
            };

            if (!needsTerminal) compilerParameters.CompilerOptions += " /target:winexe";

            var icon = target.Feed.GetIcon(Icon.MimeTypeIco, command);
            if (icon != null)
            {
                string iconPath = IconCacheProvider.GetInstance().GetIcon(icon.Href, handler);
                compilerParameters.CompilerOptions += " /win32icon:" + iconPath.EscapeArgument();
            }

            compilerParameters.CompileCSharp(
                GetRunStubCode(target, needsTerminal, command),
                GetEmbeddedResource("Stub.manifest"));
        }

        private static string GetEmbeddedResource(string name)
        {
            var assembly = Assembly.GetAssembly(typeof(StubBuilder));
            using (var stream = assembly.GetManifestResourceStream(typeof(StubBuilder), name))
                return stream.ReadToString();
        }

        /// <summary>
        /// Generates the C# to be compiled for the stub EXE.
        /// </summary>
        /// <param name="target">The application to be launched via the stub.</param>
        /// <param name="needsTerminal"><see langword="true"/> to build a CLI stub, <see langword="false"/> to build a GUI stub.</param>
        /// <param name="command">The command argument to be passed to the the "0install run" command; may be <see langword="null"/>.</param>
        /// <returns>Generated C# code.</returns>
        private static string GetRunStubCode(InterfaceFeed target, bool needsTerminal, string command)
        {
            // Build command-line
            string args = needsTerminal ? "" : "run --no-wait ";
            if (!string.IsNullOrEmpty(command)) args += "--command=" + command.EscapeArgument() + " ";
            args += target.InterfaceID.EscapeArgument();

            // Load the template code and insert variables
            string code = GetEmbeddedResource("stub.template.cs").Replace("[EXE]", Path.Combine(Locations.InstallBase, needsTerminal ? "0launch.exe" : "0install-win.exe").Replace(@"\", @"\\"));
            code = code.Replace("[ARGUMENTS]", EscapeForCode(args));
            code = code.Replace("[TITLE]", EscapeForCode(target.Feed.GetBestName(CultureInfo.CurrentUICulture, command)));
            return code;
        }

        /// <summary>
        /// Escapes a string so that is safe for substitution inside C# code
        /// </summary>
        private static string EscapeForCode(string value)
        {
            return value.Replace(@"\", @"\\").Replace("\"", "\\\"").Replace("\n", @"\n");
        }
        #endregion
    }
}
