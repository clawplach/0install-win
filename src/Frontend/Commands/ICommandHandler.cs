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
using NanoByte.Common.Tasks;
using ZeroInstall.DesktopIntegration.ViewModel;
using ZeroInstall.Store;
using ZeroInstall.Store.Feeds;
using ZeroInstall.Store.Implementations;
using ZeroInstall.Store.Model.Selection;

namespace ZeroInstall.Commands
{
    /// <summary>
    /// Callback methods to allow users to interact with <see cref="FrontendCommand"/>s.
    /// </summary>
    /// <remarks>The methods may be called from a background thread. Implementations apply appropriate thread-synchronization to update UI elements.</remarks>
    public interface ICommandHandler : ITaskHandler
    {
        /// <summary>
        /// Prepares any UI elements necessary to track the progress of <see cref="ITask"/>s.
        /// </summary>
        void ShowProgressUI();

        /// <summary>
        /// Disables any UI element created by <see cref="ShowProgressUI"/> but still leaves it visible.
        /// </summary>
        /// <remarks>Calling this method multiple times or without calling <see cref="ShowProgressUI"/> first is safe and has no effect.</remarks>
        void DisableProgressUI();

        /// <summary>
        /// Closes any UI element created by <see cref="ShowProgressUI"/>.
        /// </summary>
        /// <remarks>This may be called from a background thread. Thread-synchronization for UI elements is handled automatically.</remarks>
        void CloseProgressUI();

        /// <summary>
        /// Provides information for a potential GUI backing this handler.
        /// </summary>
        /// <param name="actionTitle">A delegate that returns a short title describing what the command being executed does.</param>
        /// <param name="delay">The number of milliseconds by which to delay the initial display of the GUI.</param>
        /// <remarks>Should be called before <see cref="ShowProgressUI"/>.</remarks>
        void SetGuiHints(Func<string> actionTitle, int delay);

        /// <summary>
        /// Shows the user the <see cref="Selections"/> made by the solver.
        /// Returns immediately. Will be ignored by non-GUI intefaces.
        /// </summary>
        /// <param name="selections">The <see cref="Selections"/> as provided by the solver.</param>
        /// <param name="feedCache">The feed cache used to retrieve feeds for additional information about implementations.</param>
        /// <remarks>Only call this between <see cref="ShowProgressUI"/> and <see cref="CloseProgressUI"/>.</remarks>
        void ShowSelections(Selections selections, IFeedCache feedCache);

        /// <summary>
        /// Allows the user to modify the interface preferences and rerun the solver if desired.
        /// Returns once the user is satisfied with her choice. Will be ignored by non-GUI intefaces.
        /// </summary>
        /// <param name="solveCallback">Called after interface preferences have been changed and the solver needs to be rerun.</param>
        /// <remarks>Only call this between <see cref="ShowProgressUI"/> and <see cref="CloseProgressUI"/>.</remarks>
        void ModifySelections(Func<Selections> solveCallback);

        /// <summary>
        /// Displays application integration options to the user.
        /// </summary>
        /// <param name="state">A View-Model for modifying the current desktop integration state.</param>
        /// <exception cref="OperationCanceledException">Thrown if the user does not want any changes to be applied.</exception>
        /// <remarks>
        ///   <para>The caller is responsible for saving any changes.</para>
        ///   <para>Only call this between <see cref="ShowProgressUI"/> and <see cref="CloseProgressUI"/>.</para>
        /// </remarks>
        void ShowIntegrateApp(IntegrationState state);

        /// <summary>
        /// Displays the configuration settings to the user.
        /// </summary>
        /// <param name="config">The configuration to show.</param>
        /// <param name="configTab">Switch to a specific tab in the configuration GUI. Has no effect in text-mode.</param>
        /// <exception cref="OperationCanceledException">Thrown if the user does not want any changes to be applied.</exception>
        /// <remarks>The caller is responsible for saving any changes.</remarks>
        void ShowConfig(Config config, ConfigTab configTab);

        /// <summary>
        /// Displays a user interface for managing <see cref="IStore"/>s.
        /// </summary>
        /// <param name="store">The <see cref="IStore"/> to manage.</param>
        /// <param name="feedCache">Information about implementations found in the <paramref name="store"/> are extracted from here.</param>
        void ManageStore(IStore store, IFeedCache feedCache);
    }
}
