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
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using NanoByte.Common;
using NanoByte.Common.Collections;
using NanoByte.Common.Controls;
using NanoByte.Common.Storage;
using NanoByte.Common.Tasks;
using NanoByte.Common.Utils;
using ZeroInstall.Commands.Properties;
using ZeroInstall.Store.Feeds;
using ZeroInstall.Store.Implementations;
using ZeroInstall.Store.ViewModel;

namespace ZeroInstall.Commands.WinForms
{
    /// <summary>
    /// Displays the content of caches (<see cref="IFeedCache"/> and <see cref="IStore"/>) in a combined tree view.
    /// </summary>
    public sealed partial class StoreManageForm : Form, ITaskHandler
    {
        #region Variables
        private readonly IStore _store;
        private readonly IFeedCache _feedCache;

        // Don't use WinForms designer for this, since it doesn't understand generics
        private readonly FilteredTreeView<StoreManageNode> _treeView = new FilteredTreeView<StoreManageNode> {Separator = '\\', CheckBoxes = true, Dock = DockStyle.Fill};
        #endregion

        #region Constructor
        /// <summary>
        /// Creates a new store management window.
        /// </summary>
        /// <param name="store">The <see cref="IStore"/> to manage.</param>
        /// <param name="feedCache">Information about implementations found in the <paramref name="store"/> are extracted from here.</param>
        public StoreManageForm(IStore store, IFeedCache feedCache)
        {
            #region Sanity checks
            if (store == null) throw new ArgumentNullException("store");
            if (feedCache == null) throw new ArgumentNullException("feedCache");
            #endregion

            _store = store;
            _feedCache = feedCache;

            InitializeComponent();
            buttonRunAsAdmin.AddShieldIcon();

            if (Locations.IsPortable) Text += @" - " + Resources.PortableMode;
            if (WindowsUtils.IsAdministrator) Text += @" (Administrator)";
            else if (WindowsUtils.IsWindowsNT) buttonRunAsAdmin.Visible = true;
            HandleCreated += delegate { Program.ConfigureTaskbar(this, Text, subCommand: ".Store.Manage", arguments: StoreMan.Name + " manage"); };

            Shown += delegate { RefreshList(); };

            _treeView.SelectedEntryChanged += OnSelectedEntryChanged;
            _treeView.CheckedEntriesChanged += OnCheckedEntriesChanged;
            splitContainer.Panel1.Controls.Add(_treeView);
        }
        #endregion

        //--------------------//

        #region Build tree list
        /// <summary>
        /// Fills the <see cref="_treeView"/> with entries.
        /// </summary>
        internal void RefreshList()
        {
            buttonRefresh.Enabled = false;
            labelLoading.Visible = true;
            refreshListWorker.RunWorkerAsync();
        }

        private void refreshListWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            _store.Flush();
            _feedCache.Flush();

            var listBuilder = new CacheNodeBuilder(_store, _feedCache);
            listBuilder.Run();
            e.Result = listBuilder;
        }

        private void refreshListWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            #region Error handling
            var ex = e.Error;
            if (ex is IOException || ex is UnauthorizedAccessException || ex is InvalidDataException)
            {
                Msg.Inform(this, ex.Message + (ex.InnerException == null ? "" : "\n" + ex.InnerException.Message), MsgSeverity.Error);
                Close();
            }
            else if (ex != null) ex.Rethrow();
            #endregion

            var nodeListBuilder = (CacheNodeBuilder)e.Result;
            var nodes = nodeListBuilder.Nodes.Select(x => new StoreManageNode(x, this));

            _treeView.Nodes = new NamedCollection<StoreManageNode>(nodes);
            textTotalSize.Text = nodeListBuilder.TotalSize.FormatBytes(CultureInfo.CurrentCulture);

            OnCheckedEntriesChanged(null, EventArgs.Empty);
            labelLoading.Visible = false;
            buttonRefresh.Enabled = true;
        }
        #endregion

        #region Event handlers
        private void OnSelectedEntryChanged(object sender, EventArgs e)
        {
            var node = (_treeView.SelectedEntry == null) ? null : _treeView.SelectedEntry.BackingNode;
            propertyGrid.SelectedObject = node;

            // Update current entry size
            var implementationEntry = node as ImplementationNode;
            textCurrentSize.Text = (implementationEntry != null) ? implementationEntry.Size.FormatBytes(CultureInfo.CurrentCulture) : "-";
        }

        private void OnCheckedEntriesChanged(object sender, EventArgs e)
        {
            if (_treeView.CheckedEntries.Count == 0)
            {
                buttonVerify.Enabled = buttonRemove.Enabled = false;
                textCheckedSize.Text = @"-";
            }
            else
            {
                buttonVerify.Enabled = buttonRemove.Enabled = true;

                // Update selected entries size
                var nodes = _treeView.CheckedEntries.Select(x => x.BackingNode);
                long totalSize = nodes.OfType<ImplementationNode>().Sum(x => x.Size);
                textCheckedSize.Text = totalSize.FormatBytes(CultureInfo.CurrentCulture);
            }
        }

        private void buttonRunAsAdmin_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(Path.Combine(Locations.InstallBase, "0install-win.exe"), "store manage") {Verb = "runas"});
                Close();
            }
                #region Error handling
            catch (IOException ex)
            {
                Msg.Inform(this, ex.Message, MsgSeverity.Error);
            }
            catch (Win32Exception ex)
            {
                Msg.Inform(this, ex.Message, MsgSeverity.Error);
            }
            #endregion
        }

        private void buttonRemove_Click(object sender, EventArgs e)
        {
            if (Msg.YesNo(this, string.Format(Resources.DeleteCheckedEntries, _treeView.CheckedEntries.Count), MsgSeverity.Warn))
            {
                try
                {
                    RunTask(new ForEachTask<CacheNode>(Resources.DeletingEntries, _treeView.CheckedEntries.Select(x => x.BackingNode), entry => entry.Delete()));
                }
                    #region Error handling
                catch (OperationCanceledException)
                {}
                catch (KeyNotFoundException ex)
                {
                    Msg.Inform(this, ex.Message, MsgSeverity.Error);
                }
                catch (IOException ex)
                {
                    Msg.Inform(this, ex.Message, MsgSeverity.Error);
                }
                catch (UnauthorizedAccessException ex)
                {
                    Msg.Inform(this, ex.Message, MsgSeverity.Error);
                }
                #endregion

                RefreshList();
            }
        }

        private void buttonVerify_Click(object sender, EventArgs e)
        {
            try
            {
                foreach (var entry in _treeView.CheckedEntries.Select(x => x.BackingNode).OfType<ImplementationNode>())
                    entry.Verify(this);
            }
                #region Error handling
            catch (OperationCanceledException)
            {}
            catch (IOException ex)
            {
                Msg.Inform(this, ex.Message, MsgSeverity.Warn);
            }
            catch (UnauthorizedAccessException ex)
            {
                Msg.Inform(this, ex.Message, MsgSeverity.Warn);
            }
            #endregion

            RefreshList();
        }

        private void buttonRefresh_Click(object sender, EventArgs e)
        {
            RefreshList();
        }

        private void buttonClose_Click(object sender, EventArgs e)
        {
            Close();
        }
        #endregion

        #region ITaskHandler
        /// <inheritdoc/>
        public CancellationToken CancellationToken { get { return default(CancellationToken); } }

        /// <summary>
        /// Always returns <see langword="false"/>.
        /// </summary>
        public bool Batch { get { return false; } set { } }

        /// <summary>
        /// Always returns 1. This ensures that information hidden by the GUI is at least retrievable from the log files.
        /// </summary>
        public int Verbosity { get { return 1; } set { } }

        /// <inheritdoc/>
        public void RunTask(ITask task)
        {
            using (var handler = new GuiTaskHandler(this)) handler.RunTask(task);
        }

        public bool AskQuestion(string question, string batchInformation = null)
        {
            return Msg.YesNo(this, question, MsgSeverity.Warn);
        }

        public void Output(string title, string information)
        {
            Msg.Inform(this, information, MsgSeverity.Info);
        }
        #endregion
    }
}
