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
using System.Linq;
using System.Xml.Serialization;
using NanoByte.Common.Collections;
using NanoByte.Common.Storage;
using NanoByte.Common.Utils;
using ZeroInstall.Store.Properties;

namespace ZeroInstall.Store.Model
{
    /// <summary>
    /// Contains a list of <see cref="Feed"/>s, reduced to only contain information relevant for overview lists.
    /// For specific <see cref="Implementation"/>s, fetch the original <see cref="Feed"/>s.
    /// Catalogs downloaded from remote locations are protected from tampering by a OpenPGP signature.
    /// </summary>
    [Description("Contains a list of feeds, reduced to only contain information relevant for overview lists.")]
    [Serializable]
    [XmlRoot("catalog", Namespace = XmlNamespace), XmlType("catalog", Namespace = XmlNamespace)]
    [XmlNamespace("xsi", XmlStorage.XsiNamespace)]
    //[XmlNamespace("feed", Feed.XmlNamespace)]
    public class Catalog : XmlUnknown, ICloneable, IEquatable<Catalog>
    {
        #region Constants
        /// <summary>
        /// The XML namespace used for storing feed catalogs. Used in combination with <see cref="Feed.XmlNamespace"/>.
        /// </summary>
        public const string XmlNamespace = "http://0install.de/schema/injector/catalog";

        /// <summary>
        /// The URI to retrieve an XSD containing the XML Schema information for this class in serialized form.
        /// </summary>
        public const string XsdLocation = XmlNamespace + "/catalog.xsd";

        /// <summary>
        /// Provides XML Editors with location hints for XSD files.
        /// </summary>
        public const string XsiSchemaLocation = XmlNamespace + " " + XsdLocation;

        /// <summary>
        /// Provides XML Editors with location hints for XSD files.
        /// </summary>
        [XmlAttribute("schemaLocation", Namespace = XmlStorage.XsiNamespace)]
        public string SchemaLocation = XsiSchemaLocation;
        #endregion

        #region Properties
        private readonly List<Feed> _feeds = new List<Feed>();

        /// <summary>
        /// A list of <see cref="Feed"/>s contained within this catalog.
        /// </summary>
        [Browsable(false)]
        [XmlElement("interface", typeof(Feed), Namespace = Feed.XmlNamespace)]
        public List<Feed> Feeds { get { return _feeds; } }
        #endregion

        #region Factory methods
        /// <summary>
        /// Merges the content of multiple <see cref="Catalog"/>s.
        /// </summary>
        /// <remarks>In case of duplicate <see cref="Feed.Uri"/>s only the first instance is kept.</remarks>
        public static Catalog Merge(IEnumerable<Catalog> catalogs)
        {
            #region Sanity checks
            if (catalogs == null) throw new ArgumentNullException("catalogs");
            #endregion

            var newCatalog = new Catalog();
            newCatalog.Feeds.AddRange(catalogs.SelectMany(catalog => catalog.Feeds).Distinct(new FeedUriComparer()));
            return newCatalog;
        }

        private class FeedUriComparer : IEqualityComparer<Feed>
        {
            public bool Equals(Feed x, Feed y)
            {
                #region Sanity checks
                if (x == null) throw new ArgumentNullException("x");
                if (y == null) throw new ArgumentNullException("y");
                #endregion

                return x.Uri == y.Uri;
            }

            public int GetHashCode(Feed obj)
            {
                #region Sanity checks
                if (obj == null) throw new ArgumentNullException("obj");
                #endregion

                if (obj.Uri == null) return 0;
                return obj.Uri.GetHashCode();
            }
        }
        #endregion

        //--------------------//

        #region Query
        /// <summary>
        /// Determines whether this catalog contains a <see cref="Feed"/> with a specific URI.
        /// </summary>
        /// <param name="uri">The <see cref="Feed.Uri"/> to look for.</param>
        /// <returns><see langword="true"/> if a matching feed was found; <see langword="false"/> otherwise.</returns>
        public bool ContainsFeed(Uri uri)
        {
            #region Sanity checks
            if (uri == null) throw new ArgumentNullException("uri");
            #endregion

            return Feeds.Any(feed => feed.Uri == uri);
        }

        /// <summary>
        /// Returns the <see cref="Feed"/> with a specific URI.
        /// </summary>
        /// <param name="uri">The <see cref="Feed.Uri"/> to look for.</param>
        /// <returns>The identified <see cref="Feed"/>.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if no <see cref="Feed"/> matching <paramref name="uri"/> was found in <see cref="Feeds"/>.</exception>
        public Feed this[Uri uri]
        {
            get
            {
                #region Sanity checks
                if (uri == null) throw new ArgumentNullException("uri");
                #endregion

                try
                {
                    return Feeds.First(feed => feed.Uri == uri);
                }
                    #region Error handling
                catch (InvalidOperationException)
                {
                    throw new KeyNotFoundException(string.Format(Resources.FeedNotInCatalog, uri));
                }
                #endregion
            }
        }

        /// <summary>
        /// Returns the <see cref="Feed"/> with a specific URI. Safe for missing elements.
        /// </summary>
        /// <param name="uri">The <see cref="Feed.Uri"/> to look for.</param>
        /// <returns>The identified <see cref="Feed"/>; <see langword="null"/> if no matching entry was found.</returns>
        public Feed GetFeed(Uri uri)
        {
            #region Sanity checks
            if (uri == null) throw new ArgumentNullException("uri");
            #endregion

            return Feeds.FirstOrDefault(feed => feed.Uri == uri);
        }

        /// <summary>
        /// Retruns the first <see cref="Feed"/> that matches a sepecific short name and has <see cref="Feed.Uri"/> set.
        /// </summary>
        /// <param name="shortName">The short name to look for. Must match either <see cref="Feed.Name"/> or <see cref="EntryPoint.BinaryName"/> of <see cref="Command.NameRun"/>.</param>
        /// <returns>The first matching <see cref="Feed"/> or <see langword="null"/> if no match was found.</returns>
        public Feed FindByShortName(string shortName)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(shortName)) throw new ArgumentNullException("shortName");
            #endregion

            return Feeds.FirstOrDefault(feed =>
            {
                if (feed.Uri == null) return false;

                // Short name matches application name
                if (StringUtils.EqualsIgnoreCase(feed.Name.Replace(' ', '-'), shortName)) return true;

                // Short name matches binary name
                var entryPoint = feed.GetEntryPoint(Command.NameRun);
                return entryPoint != null && StringUtils.EqualsIgnoreCase(entryPoint.BinaryName, shortName);
            });
        }
        #endregion

        //--------------------//

        #region Clone
        /// <summary>
        /// Creates a deep copy of this <see cref="Catalog"/> instance.
        /// </summary>
        /// <returns>The new copy of the <see cref="Catalog"/>.</returns>
        public Catalog Clone()
        {
            var catalog = new Catalog {UnknownAttributes = UnknownAttributes, UnknownElements = UnknownElements};
            catalog.Feeds.AddRange(Feeds.CloneElements());
            return catalog;
        }

        object ICloneable.Clone()
        {
            return Clone();
        }
        #endregion

        #region Equality
        /// <inheritdoc/>
        public bool Equals(Catalog other)
        {
            if (other == null) return false;
            return base.Equals(other) && Feeds.SequencedEquals(other.Feeds);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == typeof(Catalog) && Equals((Catalog)obj);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode() * 397) ^ Feeds.GetSequencedHashCode();
            }
        }
        #endregion
    }
}
