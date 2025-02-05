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
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using NanoByte.Common;
using NanoByte.Common.Tasks;
using NanoByte.Common.Utils;
using ZeroInstall.DesktopIntegration.ViewModel;
using ZeroInstall.Store;
using ZeroInstall.Store.Feeds;
using ZeroInstall.Store.Implementations;
using ZeroInstall.Store.Model.Selection;

namespace ZeroInstall.Commands.WinForms
{
    /// <summary>
    /// Wraps a <see cref="GuiCommandHandler"/> and displays it only after a certain delay (or immediately when it is required).
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable", Justification = "Disposal is handled sufficiently by GC in this case")]
    public sealed class DelayedGuiCommandHandler : MarshalNoTimeout, ICommandHandler
    {
        #region Variables
        /// <summary>The actual GUI to show with a delay.</summary>
        private volatile GuiCommandHandler _target;

        /// <summary>The number of milliseconds by which to delay the initial display of the GUI.</summary>
        private int _delay;

        /// <summary>Synchronization object used to prevent concurrent access to <see cref="_target"/>.</summary>
        private readonly object _targetLock = new object();

        /// <summary>A wait handle used to signal that <see cref="InitTarget"/> no longer needs to be called.</summary>
        private readonly AutoResetEvent _uiDone = new AutoResetEvent(false);

        /// <summary>Queues defered actions to be executed as soon as the <see cref="_target"/> is created.</summary>
        private Action<GuiCommandHandler> _onTargetCreate;
        #endregion

        #region Properties
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        /// <inheritdoc/>
        public CancellationToken CancellationToken { get { return _cancellationTokenSource.Token; } }
        #endregion

        #region Dispose
        /// <inheritdoc/>
        [SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", MessageId = "_uiDone", Justification = "Do not dispose _uiDone because of possible race conditions; let the GC handle it")]
        public void Dispose()
        {
            if (_target != null) _target.Dispose();
            _cancellationTokenSource.Dispose();
        }
        #endregion

        //--------------------//

        #region Target access
        /// <summary>
        /// Initializes the <see cref="_target"/> if it is missing (thread-safe) and returns it.
        /// </summary>
        private GuiCommandHandler InitTarget()
        {
            // Thread-safe "private" singleton
            lock (_targetLock)
            {
                if (_target != null) return _target;
                _uiDone.Set();

                // Create target but keep it hidden until all defered actions are complete (ensures correct order)
                var newTarget = new GuiCommandHandler(_cancellationTokenSource);
                if (_onTargetCreate != null) _onTargetCreate(newTarget);
                return _target = newTarget;
            }
        }

        /// <summary>
        /// Applies an action to the <see cref="_target"/> as soon as it is created
        /// </summary>
        private void ApplyToTarget(Action<GuiCommandHandler> action)
        {
            // Thread-safe "private" singleton
            lock (_targetLock)
            {
                if (_target != null) action(_target);
                else _onTargetCreate += action;
            }
        }
        #endregion

        #region UI control
        /// <inheritdoc/>
        public void ShowProgressUI()
        {
            _onTargetCreate += target => target.ShowProgressUI();

            if (_delay == 0) InitTarget();
            else
            {
                ProcessUtils.RunAsync(() =>
                {
                    // Wait for delay to initialize target, unless some interrupt event cause the UI to be created ahead of time
                    if (!_uiDone.WaitOne(_delay, exitContext: false)) InitTarget();
                }, "DelayedGuiHandler.InitTarget");
            }
        }

        /// <inheritdoc/>
        public void CloseProgressUI()
        {
            _uiDone.Set();
            ApplyToTarget(target => target.CloseProgressUI());
        }
        #endregion

        #region Pass-through
        // Keep local read cache, assume no inner changes
        private int _verbosity;

        /// <inheritdoc/>
        public int Verbosity
        {
            get { return _verbosity; }
            set
            {
                _verbosity = value;
                ApplyToTarget(target => target.Verbosity = value);
            }
        }

        // Keep local read cache, assume no inner changes
        private bool _batch;

        /// <inheritdoc/>
        public bool Batch
        {
            get { return _batch; }
            set
            {
                _batch = value;
                ApplyToTarget(target => target.Batch = value);
            }
        }

        /// <inheritdoc/>
        public void SetGuiHints(Func<string> actionTitle, int delay)
        {
            _delay = delay;
            ApplyToTarget(target => target.SetGuiHints(actionTitle, delay));
        }

        /// <inheritdoc/>
        public void RunTask(ITask task)
        {
            #region Sanity checks
            if (task == null) throw new ArgumentNullException("task");
            #endregion

            InitTarget().RunTask(task);
        }

        /// <inheritdoc/>
        public void DisableProgressUI()
        {
            ApplyToTarget(target => target.DisableProgressUI());
        }

        /// <inheritdoc/>
        public bool AskQuestion(string question, string batchInformation = null)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(question)) throw new ArgumentNullException("question");
            #endregion

            return InitTarget().AskQuestion(question, batchInformation);
        }

        /// <inheritdoc/>
        public void ShowSelections(Selections selections, IFeedCache feedCache)
        {
            #region Sanity checks
            if (selections == null) throw new ArgumentNullException("selections");
            if (feedCache == null) throw new ArgumentNullException("feedCache");
            #endregion

            ApplyToTarget(target => target.ShowSelections(selections, feedCache));
        }

        /// <inheritdoc/>
        public void ModifySelections(Func<Selections> solveCallback)
        {
            #region Sanity checks
            if (solveCallback == null) throw new ArgumentNullException("solveCallback");
            #endregion

            InitTarget().ModifySelections(solveCallback);
        }

        /// <inheritdoc/>
        public void Output(string title, string information)
        {
            InitTarget().Output(title, information);
        }

        /// <inheritdoc/>
        public void ShowIntegrateApp(IntegrationState state)
        {
            #region Sanity checks
            if (state == null) throw new ArgumentNullException("state");
            #endregion

            InitTarget().ShowIntegrateApp(state);
        }

        /// <inheritdoc/>
        public void ShowConfig(Config config, ConfigTab configTab)
        {
            #region Sanity checks
            if (config == null) throw new ArgumentNullException("config");
            #endregion

            InitTarget().ShowConfig(config, configTab);
        }

        /// <inheritdoc/>
        public void ManageStore(IStore store, IFeedCache feedCache)
        {
            InitTarget().ManageStore(store, feedCache);
        }
        #endregion
    }
}
