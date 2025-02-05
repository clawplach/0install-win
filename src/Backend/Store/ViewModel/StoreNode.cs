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

using System.ComponentModel;
using ZeroInstall.Store.Implementations;

namespace ZeroInstall.Store.ViewModel
{
    /// <summary>
    /// Models information about elements in a cache for display in a UI.
    /// </summary>
    public abstract class StoreNode : CacheNode
    {
        #region Dependencies
        /// <summary>The store containing the element.</summary>
        protected readonly IStore Store;

        /// <summary>
        /// Creates a new store node.
        /// </summary>
        /// <param name="store">The store containing the element.</param>
        protected StoreNode(IStore store)
        {
            Store = store;
        }
        #endregion

        /// <summary>
        /// The file system path of the element.
        /// </summary>
        [Description("The file system path of the element.")]
        public abstract string Path { get; }
    }
}
