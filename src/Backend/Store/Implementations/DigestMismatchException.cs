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
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Text;
using NanoByte.Common.Dispatch;
using ZeroInstall.Store.Model;
using ZeroInstall.Store.Properties;

namespace ZeroInstall.Store.Implementations
{
    /// <summary>
    /// Indicates an <see cref="Store.Model.Implementation"/> directory does not match a <see cref="ManifestDigest"/>.
    /// </summary>
    [Serializable]
    public sealed class DigestMismatchException : Exception
    {
        #region Properties
        /// <summary>
        /// The hash value the <see cref="Store.Model.Implementation"/> was supposed to have.
        /// </summary>
        public string ExpectedDigest { get; private set; }

        /// <summary>
        /// The <see cref="Manifest"/> that resulted in the <see cref="ExpectedDigest"/>.
        /// </summary>
        public Manifest ExpectedManifest { get; private set; }

        /// <summary>
        /// The hash value that was actually calculated.
        /// </summary>
        public string ActualDigest { get; private set; }

        /// <summary>
        /// The <see cref="Manifest"/> that resulted in the <see cref="ActualDigest"/>.
        /// </summary>
        public Manifest ActualManifest { get; private set; }
        #endregion

        #region Constructor
        /// <summary>
        /// Creates a new digest mismatch exception.
        /// </summary>
        /// <param name="expectedDigest">The digest value the <see cref="Store.Model.Implementation"/> was supposed to have.</param>
        /// <param name="actualDigest">The digest value that was actually calculated.</param>
        /// <param name="expectedManifest">The <see cref="Manifest"/> that resulted in the <paramref name="expectedDigest"/>; may be <seealso langword="null"/>.</param>
        /// <param name="actualManifest">The <see cref="Manifest"/> that resulted in the <paramref name="actualDigest"/>.</param>
        public DigestMismatchException(string expectedDigest = null, string actualDigest = null, Manifest expectedManifest = null, Manifest actualManifest = null)
            : base(GetMessage(expectedDigest, actualDigest, expectedManifest, actualManifest))
        {
            ExpectedDigest = expectedDigest;
            ActualDigest = actualDigest;
            ExpectedManifest = expectedManifest;
            ActualManifest = actualManifest;
        }

        private static string GetMessage(string expectedDigest, string actualDigest, Manifest expectedManifest, Manifest actualManifest)
        {
            var builder = new StringBuilder(Resources.DigestMismatch);
            if (!string.IsNullOrEmpty(expectedDigest)) builder.AppendLine(string.Format(Resources.DigestMismatchExpectedDigest, expectedDigest));
            if (!string.IsNullOrEmpty(actualDigest)) builder.AppendLine(string.Format(Resources.DigestMismatchActualDigest, actualDigest));

            if (expectedManifest != null && actualManifest != null)
            { // Diff
                Merge.TwoWay(expectedManifest, actualManifest,
                    added: node => builder.AppendLine("unexpected: " + node),
                    removed: node => builder.AppendLine("missing: " + node));
            }
            else
            {
                if (expectedManifest != null) builder.AppendLine(string.Format(Resources.DigestMismatchExpectedManifest, expectedManifest));
                if (actualManifest != null) builder.AppendLine(string.Format(Resources.DigestMismatchActualManifest, actualManifest));
            }
            return builder.ToString();
        }

        /// <inheritdoc/>
        public DigestMismatchException()
            : base(GetMessage(null, null, null, null))
        {}

        /// <inheritdoc/>
        public DigestMismatchException(string message) : base(message)
        {}

        /// <inheritdoc/>
        public DigestMismatchException(string message, Exception innerException) : base(message, innerException)
        {}

        /// <summary>
        /// Deserializes an exception.
        /// </summary>
        private DigestMismatchException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            #region Sanity checks
            if (info == null) throw new ArgumentNullException("info");
            #endregion

            ExpectedDigest = info.GetString("ExpectedDigest");
            ExpectedManifest = (Manifest)info.GetValue("ExpectedManifest", typeof(Manifest));
            ActualDigest = info.GetString("ActualDigest");
            ActualManifest = (Manifest)info.GetValue("ActualManifest", typeof(Manifest));
        }
        #endregion

        #region Serialization
        /// <inheritdoc/>
        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            #region Sanity checks
            if (info == null) throw new ArgumentNullException("info");
            #endregion

            info.AddValue("ExpectedDigest", ExpectedDigest);
            info.AddValue("ExpectedManifest", ExpectedManifest);
            info.AddValue("ActualDigest", ActualDigest);
            info.AddValue("ActualManifest", ActualManifest);

            base.GetObjectData(info, context);
        }
        #endregion
    }
}
