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
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using NanoByte.Common;
using NanoByte.Common.Controls;
using NanoByte.Common.Utils;
using ZeroInstall.Updater.Properties;

namespace ZeroInstall.Updater.WinForms
{
    /// <summary>
    /// Executes and tracks the progress of an <see cref="UpdateProcess"/>.
    /// </summary>
    public sealed partial class MainForm : Form
    {
        #region Variables
        /// <summary>The update process to execute and track.</summary>
        private readonly UpdateProcess _updateProcess;

        /// <summary>Indicates whether the updater has alread respawned itself as an administator. This is used to prevent infinite loops.</summary>
        private readonly bool _rerun;

        /// <summary>Indicates whether the updater shall restart the "0install central" GUI after the update.</summary>
        private readonly bool _restartCentral;
        #endregion

        #region Constructor
        /// <summary>
        /// Creates a new update GUI.
        /// </summary>
        /// <param name="updateProcess">The update process to execute and track.</param>
        /// <param name="rerun">Indicates whether the updater has alread respawned itself as an administator. This is used to prevent infinite loops.</param>
        /// <param name="restartCentral">Indicates whether the updater shall restart the "0install central" GUI after the update.</param>
        public MainForm(UpdateProcess updateProcess, bool rerun, bool restartCentral)
        {
            _updateProcess = updateProcess;
            _rerun = rerun;
            _restartCentral = restartCentral;

            InitializeComponent();
        }
        #endregion

        #region Startup
        private void MainForm_Shown(object sender, EventArgs e)
        {
            WindowsUtils.SetProgressState(Handle, WindowsUtils.TaskbarProgressBarState.Indeterminate);

            backgroundWorker.RunWorkerAsync();
        }
        #endregion

        //--------------------//

        #region Background worker
        private void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            SetStatus(Resources.MutexWait);
            _updateProcess.MutexAquire();

            try
            {
                SetStatus(Resources.StopService);
                bool serviceWasRunning = _updateProcess.StopService();

                SetStatus(Resources.CopyFiles);
                Retry:
                try
                {
                    _updateProcess.CopyFiles();
                }
                    #region Error handling
                catch (IOException ex)
                {
                    // If already admin, ask whether to retry
                    if (WindowsUtils.IsAdministrator && Msg.YesNo(this, ex.Message + "\n" + Resources.TryAgain, MsgSeverity.Error)) goto Retry;
                    else throw;
                }
                catch (UnauthorizedAccessException ex)
                {
                    // If already admin, ask whether to retry
                    if (WindowsUtils.IsAdministrator && Msg.YesNo(this, ex.Message + "\n" + Resources.TryAgain, MsgSeverity.Error)) goto Retry;
                    else throw;
                }
                #endregion

                SetStatus(Resources.DeleteFiles);
                _updateProcess.DeleteFiles();

                if (_updateProcess.IsInnoSetup)
                {
                    SetStatus(Resources.RunNgen);
                    _updateProcess.RunNgen();

                    SetStatus(Resources.UpdateRegistry);
                    _updateProcess.UpdateRegistry();
                }

                SetStatus(Resources.FixPermissions);
                _updateProcess.FixPermissions();

                _updateProcess.MutexRelease(); // Must release blocking mutexes before restarting the service
                if (serviceWasRunning)
                {
                    SetStatus(Resources.StartService);
                    _updateProcess.StartService();
                }

                SetStatus(Resources.Done);
            }
            catch (UnauthorizedAccessException ex)
            {
                // Do not try to elevate endlessly
                if (_rerun || WindowsUtils.IsAdministrator) throw;

                Log.Info("Elevation request triggered by:");
                Log.Warn(ex);

                SetStatus(Resources.RerunElevated);
                _updateProcess.MutexRelease(); // Must release blocking mutexes in case the child process needs them
                RerunElevated();

                SetStatus(Resources.Done);
            }
        }

        private void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                if (e.Error is IOException || e.Error is UnauthorizedAccessException || e.Error is InvalidOperationException)
                { // Expected error
                    Msg.Inform(null, (e.Error.InnerException ?? e.Error).Message, MsgSeverity.Error);
                }
                else
                { // Unexpected error
                    ErrorReportForm.Report(e.Error, new Uri("http://0install.de/error-report/"));
                }
            }

            if (_restartCentral && !_rerun) _updateProcess.RestartCentral();
            Thread.Sleep(2000);
            Close();
        }

        /// <summary>
        /// Changes the current status message displayed to the user.
        /// </summary>
        /// <param name="message">The message to display to the user.</param>
        private void SetStatus(string message)
        {
            Invoke(new Action(() => statusLabel.Text = message));
        }
        #endregion

        #region Spawn processes
        /// <summary>
        /// Reruns the updater using elevated permissions (as administartor).
        /// </summary>
        private void RerunElevated()
        {
            try
            {
                var startInfo = new ProcessStartInfo(Application.ExecutablePath, new[] {_updateProcess.Source, _updateProcess.NewVersion.ToString(), _updateProcess.Target, "--rerun"}.JoinEscapeArguments()) {Verb = "runas"};
                using (var process = Process.Start(startInfo))
                    process.WaitForExit();
            }
            catch (Win32Exception)
            {}
        }
        #endregion
    }
}
