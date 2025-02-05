﻿/*
 * Copyright 2010-2014 Bastian Eicher, Simon E. Silva Lauinger
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
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using NanoByte.Common;
using NanoByte.Common.Dispatch;
using NanoByte.Common.Tasks;
using NanoByte.Common.Utils;
using ZeroInstall.Store.Implementations.Archives;
using ZeroInstall.Store.Model;
using ZeroInstall.Store.Properties;

namespace ZeroInstall.Store.Implementations
{
    /// <summary>
    /// Models a cache directory that stores <see cref="Store.Model.Implementation"/>s, each in its own sub-directory named by its <see cref="ManifestDigest"/>.
    /// </summary>
    /// <remarks>The represented store data is mutable but the class itself is immutable.</remarks>
    public class DirectoryStore : MarshalNoTimeout, IStore, IEquatable<DirectoryStore>
    {
        #region Properties
        /// <summary>
        /// The directory containing the cached <see cref="Store.Model.Implementation"/>s.
        /// </summary>
        public string DirectoryPath { get; private set; }
        #endregion

        #region Constructor
        /// <summary>
        /// Creates a new store using a specific path to a cache directory.
        /// </summary>
        /// <param name="path">A fully qualified directory path. The directory will be created if it doesn't exist yet.</param>
        /// <exception cref="IOException">Thrown if the directory <paramref name="path"/> could not be created or if the underlying filesystem can not store file-changed times accurate to the second.</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown if creating the directory <paramref name="path"/> is not permitted.</exception>
        public DirectoryStore(string path)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException("path");
            #endregion

            try
            {
                path = Path.GetFullPath(path);
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            }
                #region Error handling
            catch (ArgumentException ex)
            {
                // Wrap exception since only certain exception types are allowed
                throw new IOException(ex.Message, ex);
            }
            catch (NotSupportedException ex)
            {
                // Wrap exception since only certain exception types are allowed
                throw new IOException(ex.Message, ex);
            }
            #endregion

            DirectoryPath = path;

            // Ensure the store is backed by a filesystem that can store file-changed times accurate to the second (otherwise ManifestDigets will break)
            try
            {
                if (FileUtils.DetermineTimeAccuracy(path) > 0)
                    throw new IOException(Resources.InsufficientFSTimeAccuracy);
            }
            catch (UnauthorizedAccessException)
            {
                // Ignore if we cannot verify the timestamp accuracy of read-only stores
            }
        }
        #endregion

        //--------------------//

        #region Temp dir
        /// <summary>
        /// Creates a temporary directory within <see cref="DirectoryPath"/>.
        /// </summary>
        /// <returns>The path to the new temporary directory.</returns>
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "Returns a new value on each call")]
        protected virtual string GetTempDir()
        {
            string path = Path.Combine(DirectoryPath, Path.GetRandomFileName());
            Directory.CreateDirectory(path);
            return path;
        }

        /// <summary>
        /// Deletes a temporary directory.
        /// </summary>
        /// <param name="path">The path to the temporary directory.</param>
        protected virtual void DeleteTempDir(string path)
        {
            if (Directory.Exists(path)) Directory.Delete(path, recursive: true);
        }
        #endregion

        #region Verify and add
        private readonly object _renameLock = new object();

        /// <summary>
        /// Verifies the <see cref="ManifestDigest"/> of a directory temporarily stored inside the cache and moves it to the final location if it passes.
        /// </summary>
        /// <param name="tempID">The temporary identifier of the directory inside the cache.</param>
        /// <param name="expectedDigest">The digest the <see cref="Store.Model.Implementation"/> is supposed to match.</param>
        /// <param name="handler">A callback object used when the the user is to be informed about progress.</param>
        /// <exception cref="DigestMismatchException">Thrown if the temporary directory doesn't match the <paramref name="expectedDigest"/>.</exception>
        /// <exception cref="IOException">Thrown if <paramref name="tempID"/> cannot be moved or the digest cannot be calculated.</exception>
        /// <exception cref="ImplementationAlreadyInStoreException">Thrown if there is already an <see cref="Store.Model.Implementation"/> with the specified <paramref name="expectedDigest"/> in the store.</exception>
        [SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly")]
        protected virtual void VerifyAndAdd(string tempID, ManifestDigest expectedDigest, ITaskHandler handler)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(tempID)) throw new ArgumentNullException("tempID");
            if (handler == null) throw new ArgumentNullException("handler");
            #endregion

            // Determine the digest method to use
            string expectedDigestValue = expectedDigest.Best;

            // Determine the source and target directories
            string source = Path.Combine(DirectoryPath, tempID);
            string target = Path.Combine(DirectoryPath, expectedDigestValue);

            // Calculate the actual digest, compare it with the expected one and create a manifest file
            VerifyDirectory(source, expectedDigest, handler).Save(Path.Combine(source, ".manifest"));

            lock (_renameLock) // Prevent race-conditions when adding the same digest twice
            {
                if (Directory.Exists(target)) throw new ImplementationAlreadyInStoreException(expectedDigest);

                // Move directory to final store destination
                try
                {
                    Directory.Move(source, target);
                }
                catch (IOException ex)
                {
                    // TODO: Make language independent
                    if (ex.Message.Contains("already exists")) throw new ImplementationAlreadyInStoreException(expectedDigest);
                    throw;
                }
            }

            // Prevent any further changes to the directory
            try
            {
                FileUtils.EnableWriteProtection(target);
            }
                #region Error handling
            catch (IOException)
            {
                Log.Warn(string.Format(Resources.UnableToWriteProtect, target));
            }
            catch (UnauthorizedAccessException)
            {
                Log.Warn(string.Format(Resources.UnableToWriteProtect, target));
            }
            #endregion
        }
        #endregion

        #region Verify directory
        /// <summary>
        /// Verifies the <see cref="ManifestDigest"/> of a directory.
        /// </summary>
        /// <param name="directory">The directory to generate a <see cref="Manifest"/> for.</param>
        /// <param name="expectedDigest">The digest the <see cref="Manifest"/> of the <paramref name="directory"/> should have.</param>
        /// <param name="handler">A callback object used when the the user is to be informed about progress.</param>
        /// <returns>The generated <see cref="Manifest"/>.</returns>
        /// <exception cref="IOException">Thrown if the <paramref name="directory"/> could not be processed.</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown if read access to the <paramref name="directory"/> is not permitted.</exception>
        /// <exception cref="DigestMismatchException">Thrown if the <paramref name="directory"/> doesn't match the <paramref name="expectedDigest"/>.</exception>
        [SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly")]
        public static Manifest VerifyDirectory(string directory, ManifestDigest expectedDigest, ITaskHandler handler)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(directory)) throw new ArgumentNullException("directory");
            if (handler == null) throw new ArgumentNullException("handler");
            #endregion

            string expectedDigestValue = expectedDigest.Best;
            var format = ManifestFormat.FromPrefix(expectedDigestValue);

            var actualManifest = Manifest.Generate(directory, format, handler, expectedDigest);
            string actualDigestValue = actualManifest.CalculateDigest();

            string manifestFilePath = Path.Combine(directory, ".manifest");
            var expectedManifest = File.Exists(manifestFilePath) ? Manifest.Load(manifestFilePath, format) : null;

            if (actualDigestValue != expectedDigestValue)
            {
                throw new DigestMismatchException(
                    expectedDigestValue,
                    actualDigestValue,
                    // Only log the complete manifests in verbose mode
                    (handler.Verbosity > 0) ? expectedManifest : null,
                    (handler.Verbosity > 0) ? actualManifest : null);
            }

            return actualManifest;
        }
        #endregion

        //--------------------//

        #region List all
        /// <inheritdoc/>
        public IEnumerable<ManifestDigest> ListAll()
        {
            if (!Directory.Exists(DirectoryPath)) return Enumerable.Empty<ManifestDigest>();

            var result = new List<ManifestDigest>();
            foreach (string path in FileUtils.GetDirectories(DirectoryPath))
            {
                try
                {
                    result.Add(new ManifestDigest(Path.GetFileName(path)));
                }
                catch (NotSupportedException)
                {}
            }
            return result;
        }

        /// <inheritdoc/>
        public IEnumerable<string> ListAllTemp()
        {
            if (!Directory.Exists(DirectoryPath)) return Enumerable.Empty<string>();

            var result = new List<string>();
            foreach (string path in FileUtils.GetDirectories(DirectoryPath))
            {
                try
                {
                    // ReSharper disable once ObjectCreationAsStatement
                    new ManifestDigest(Path.GetFileName(path));
                }
                catch (NotSupportedException)
                {
                    // Anything that is not a valid digest is considered a temp directory
                    result.Add(path);
                }
            }
            return result;
        }
        #endregion

        #region Contains
        /// <inheritdoc/>
        public bool Contains(ManifestDigest manifestDigest)
        {
            // Check for all supported digest algorithms
            return manifestDigest.AvailableDigests.Any(digest => Directory.Exists(Path.Combine(DirectoryPath, digest)));
        }

        /// <inheritdoc/>
        public bool Contains(string directory)
        {
            return Directory.Exists(Path.Combine(DirectoryPath, directory));
        }

        /// <inheritdoc/>
        public void Flush()
        {
            // No internal caching
        }
        #endregion

        #region Get
        /// <inheritdoc/>
        public string GetPath(ManifestDigest manifestDigest)
        {
            // Check for all supported digest algorithms
            return manifestDigest.AvailableDigests.Select(digest => Path.Combine(DirectoryPath, digest)).FirstOrDefault(Directory.Exists);
        }
        #endregion

        #region Add
        /// <inheritdoc/>
        public void AddDirectory(string path, ManifestDigest manifestDigest, ITaskHandler handler)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException("path");
            if (handler == null) throw new ArgumentNullException("handler");
            #endregion

            if (Contains(manifestDigest)) throw new ImplementationAlreadyInStoreException(manifestDigest);

            // Copy to temporary directory inside the cache so it can be validated safely (no manipulation of directory while validating)
            string tempDir = GetTempDir();
            try
            {
                // Copy the source directory inside the cache so it can be validated safely (no manipulation of directory while validating)
                try
                {
                    handler.RunTask(new CopyDirectory(path, tempDir) {Tag = manifestDigest});
                }
                    #region Error handling
                catch (IOException ex)
                {
                    // Wrap too generic exceptions
                    // TODO: Make language independent
                    if (ex.Message.StartsWith("Access") && ex.Message.EndsWith("is denied.")) throw new UnauthorizedAccessException(ex.Message, ex);

                    // Pass other exceptions through
                    throw;
                }
                #endregion

                VerifyAndAdd(Path.GetFileName(tempDir), manifestDigest, handler);
            }
            finally
            {
                // Remove temporary directory before passing exception on
                DeleteTempDir(tempDir);
            }
        }

        /// <inheritdoc/>
        public void AddArchives(IEnumerable<ArchiveFileInfo> archiveInfos, ManifestDigest manifestDigest, ITaskHandler handler)
        {
            #region Sanity checks
            if (archiveInfos == null) throw new ArgumentNullException("archiveInfos");
            if (handler == null) throw new ArgumentNullException("handler");
            #endregion

            if (Contains(manifestDigest)) throw new ImplementationAlreadyInStoreException(manifestDigest);

            // Extract to temporary directory inside the cache so it can be validated safely (no manipulation of directory while validating)
            string tempDir = GetTempDir();
            try
            {
                // Extract archives "over each other" in order
                foreach (var archiveInfo in archiveInfos)
                {
                    using (var extractor = Extractor.FromFile(archiveInfo.Path, tempDir, archiveInfo.MimeType, archiveInfo.StartOffset))
                    {
                        extractor.SubDir = archiveInfo.SubDir;
                        extractor.Destination = archiveInfo.Destination;
                        extractor.Tag = manifestDigest;
                        handler.RunTask(extractor);
                    }
                }

                VerifyAndAdd(Path.GetFileName(tempDir), manifestDigest, handler);
            }
            finally
            {
                // Remove extracted directory if validation or something else failed
                DeleteTempDir(tempDir);
            }
        }
        #endregion

        #region Remove
        /// <inheritdoc/>
        public virtual void Remove(ManifestDigest manifestDigest)
        {
            string path = GetPath(manifestDigest);
            if (path == null) throw new ImplementationNotFoundException(manifestDigest);

            FileUtils.DisableWriteProtection(path);

            // Move the directory to be deleted to a temporary directory to ensure the removal operation is atomic
            string tempDir = Path.Combine(DirectoryPath, Path.GetRandomFileName());
            Directory.Move(path, tempDir);

            Directory.Delete(tempDir, recursive: true);
        }
        #endregion

        #region Optimise

        #region Structs
        private struct DedupKey
        {
            public readonly long Size;
            public readonly long LastModified;
            public readonly ManifestFormat Format;
            public readonly string Digest;

            public DedupKey(long size, long lastModified, ManifestFormat format, string digest)
            {
                Size = size;
                LastModified = lastModified;
                Format = format;
                Digest = digest;
            }

            public override bool Equals(object obj)
            {
                if (!(obj is DedupKey)) return false;
                var other = (DedupKey)obj;
                return Size == other.Size && LastModified == other.LastModified && Format.Equals(other.Format) && string.Equals(Digest, other.Digest);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hashCode = Size.GetHashCode();
                    hashCode = (hashCode * 397) ^ LastModified.GetHashCode();
                    hashCode = (hashCode * 397) ^ Format.GetHashCode();
                    hashCode = (hashCode * 397) ^ Digest.GetHashCode();
                    return hashCode;
                }
            }
        }
        #endregion

        /// <inheritdoc/>
        public virtual long Optimise(ITaskHandler handler)
        {
            #region Sanity checks
            if (handler == null) throw new ArgumentNullException("handler");
            #endregion

            if (!Directory.Exists(DirectoryPath)) return 0;

            var fileHashes = new Dictionary<DedupKey, string>();
            long savedSpace = 0;
            handler.RunTask(new ForEachTask<ManifestDigest>(
                name: string.Format(Resources.FindingDuplicateFiles, DirectoryPath),
                target: ListAll(),
                work: manifestDigest =>
                {
                    var manifest = Manifest.Load(
                        Path.Combine(GetPath(manifestDigest), ".manifest"),
                        ManifestFormat.FromPrefix(manifestDigest.Best));

                    string currentDirectory = "";
                    new AggregateDispatcher<ManifestNode>
                    {
                        (ManifestDirectory x) => { currentDirectory = x.FullPath; },
                        (ManifestFileBase x) =>
                        {
                            if (x.Size == 0) return;

                            var key = new DedupKey(x.Size, x.ModifiedTime, manifest.Format, x.Digest);
                            string path = manifestDigest.Best + currentDirectory + '/' + x.FileName;

                            string existingPath;
                            if (fileHashes.TryGetValue(key, out existingPath))
                            {
                                if (CreateHardlink(path, existingPath)) savedSpace += x.Size;
                            }
                            else fileHashes.Add(key, path);
                        }
                    }.Dispatch(manifest);
                }));
            return savedSpace;
        }

        /// <summary>
        /// Creates a hardlink between two implementation files.
        /// </summary>
        /// <param name="source">The Unix-style path to the hardlink to create, relative to <see cref="DirectoryPath"/>. Existing file will be replaced.</param>
        /// <param name="destination">The Unix-style path to existing file the hardlink shall point to, relative to <see cref="DirectoryPath"/>.</param>
        /// <returns><see langword="true"/> the hardlink was created; <see langword="false"/> if the files were already linked.</returns>
        private bool CreateHardlink(string source, string destination)
        {
            string tempFile = Path.Combine(DirectoryPath, Path.GetRandomFileName());
            string destinationFile = Path.Combine(DirectoryPath, FileUtils.UnifySlashes(destination));
            string sourceFile = Path.Combine(DirectoryPath, FileUtils.UnifySlashes(source));
            string sourceImplementation = Path.Combine(DirectoryPath, source.GetLeftPartAtFirstOccurrence('/'));
            string destinationImplementation = Path.Combine(DirectoryPath, destination.GetLeftPartAtFirstOccurrence('/'));

            if (FileUtils.AreHardlinked(sourceFile, destinationFile)) return false;

            FileUtils.DisableWriteProtection(sourceImplementation);
            if (destinationImplementation != sourceImplementation) FileUtils.DisableWriteProtection(destinationImplementation);
            try
            {
                FileUtils.CreateHardlink(tempFile, destinationFile);
                FileUtils.Replace(tempFile, sourceFile);
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
                FileUtils.EnableWriteProtection(sourceImplementation);
                if (destinationImplementation != sourceImplementation) FileUtils.EnableWriteProtection(destinationImplementation);
            }
            return true;
        }
        #endregion

        #region Verify
        /// <inheritdoc/>
        [SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly")]
        public virtual void Verify(ManifestDigest manifestDigest, ITaskHandler handler)
        {
            #region Sanity checks
            if (handler == null) throw new ArgumentNullException("handler");
            #endregion

            if (!Contains(manifestDigest)) throw new ImplementationNotFoundException(manifestDigest);

            string target = Path.Combine(DirectoryPath, manifestDigest.Best);
            try
            {
                VerifyDirectory(target, manifestDigest, handler);
            }
            catch (DigestMismatchException ex)
            {
                Log.Error(ex);
                if (handler.AskQuestion(
                    question: string.Format(Resources.ImplementationDamaged + Environment.NewLine + Resources.ImplementationDamagedAskRemove, ex.ExpectedDigest),
                    batchInformation: string.Format(Resources.ImplementationDamaged + Environment.NewLine + Resources.ImplementationDamagedBatchInformation, ex.ExpectedDigest)))
                    handler.RunTask(new SimpleTask(string.Format(Resources.DeletingImplementation, ex.ExpectedDigest), () => Remove(new ManifestDigest(ex.ExpectedDigest))));
            }

            // Reseal the directory in case the write protection got lost
            try
            {
                FileUtils.EnableWriteProtection(target);
            }
                #region Error handling
            catch (IOException)
            {}
            catch (UnauthorizedAccessException)
            {}
            #endregion
        }
        #endregion

        //--------------------//

        #region Conversion
        /// <summary>
        /// Returns <see cref="DirectoryPath"/>. Not safe for parsing!
        /// </summary>
        public override string ToString()
        {
            return DirectoryPath;
        }
        #endregion

        #region Equality
        /// <inheritdoc/>
        public bool Equals(DirectoryStore other)
        {
            if (other == null) return false;
            return DirectoryPath == other.DirectoryPath;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == typeof(DirectoryStore) && Equals((DirectoryStore)obj);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return (DirectoryPath != null ? DirectoryPath.GetHashCode() : 0);
        }
        #endregion
    }
}
