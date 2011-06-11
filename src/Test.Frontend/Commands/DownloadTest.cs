/*
 * Copyright 2010-2011 Bastian Eicher
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
using Common.Storage;
using NUnit.Framework;
using ZeroInstall.Fetchers;
using ZeroInstall.Injector.Solver;
using ZeroInstall.Model;

namespace ZeroInstall.Commands
{
    /// <summary>
    /// Contains code for testing <see cref="Download"/>.
    /// </summary>
    [TestFixture]
    public class DownloadTest : SelectionTest
    {
        /// <inheritdoc/>
        protected override CommandBase GetCommand()
        {
            return new Download(Policy);
        }

        [Test(Description = "Ensures all options are parsed and handled correctly.")]
        public override void TestNormal()
        {
            var requirements = RequirementsTest.CreateTestRequirements();
            var selections = SelectionsTest.CreateTestSelections();
            var testFeed1 = FeedTest.CreateTestFeed();
            var testImplementation1 = testFeed1.GetImplementation(selections.Implementations[0].ID);

            var testImplementation2 = new Implementation { ID = "id2", ManifestDigest = new ManifestDigest("sha256=abc"), Version = new ImplementationVersion("1.0") };
            var testFeed2 = new Feed
            {
                Uri = new Uri("http://0install.de/feeds/test/test2.xml"),
                Name = "MyApp",
                Elements = {testImplementation2}
            };

            var refreshPolicy = Policy.ClonePolicy();
            refreshPolicy.FeedManager.Refresh = true;

            var args = new[] {"http://0install.de/feeds/test/test1.xml", "--command=\"command name\"", "--os=Windows", "--cpu=i586", "--not-before=1.0", "--before=2.0"};
            Command.Parse(args);

            SolverMock.ExpectAndReturn("Solve", selections, requirements, Policy, false); // First Solve()
            CacheMock.ExpectAndReturn("GetFeed", testFeed1, new Uri("http://0install.de/feeds/test/sub1.xml")); // Get feeds from cache to determine uncached implementations
            CacheMock.ExpectAndReturn("GetFeed", testFeed2, new Uri("http://0install.de/feeds/test/sub2.xml"));
            SolverMock.ExpectAndReturn("Solve", selections, requirements, refreshPolicy, false); // Refresh Solve() because there are uncached implementations
            CacheMock.ExpectAndReturn("GetFeed", testFeed1, new Uri("http://0install.de/feeds/test/sub1.xml")); // Redetermine uncached implementations after refresh Solve()
            CacheMock.ExpectAndReturn("GetFeed", testFeed2, new Uri("http://0install.de/feeds/test/sub2.xml"));

            // Download uncached implementations
            var request = new FetchRequest(new[] {testImplementation1, testImplementation2}, Policy.Handler);
            FetcherMock.Expect("Start", request);
            FetcherMock.Expect("Join", request);

            Assert.AreEqual(0, Command.Execute());
        }

        [Test(Description = "Ensures local Selections XMLs are correctly detected and parsed.")]
        public override void TestImportSelections()
        {
            var testFeed1 = FeedTest.CreateTestFeed();
            var testFeed2 = FeedTest.CreateTestFeed();
            CacheMock.ExpectAndReturn("GetFeed", testFeed1, new Uri("http://0install.de/feeds/test/sub1.xml")); // Get feeds from cache to determine uncached implementations
            CacheMock.ExpectAndReturn("GetFeed", testFeed2, new Uri("http://0install.de/feeds/test/sub2.xml"));
            CacheMock.ExpectAndReturn("GetFeed", testFeed1, new Uri("http://0install.de/feeds/test/sub1.xml")); // Redetermine uncached implementations even though Solve() doesn't do anything for selections documents
            CacheMock.ExpectAndReturn("GetFeed", testFeed2, new Uri("http://0install.de/feeds/test/sub2.xml"));

            var selections = SelectionsTest.CreateTestSelections();
            using (var tempFile = new TemporaryFile("0install-unit-tests"))
            {
                selections.Save(tempFile.Path);
                var args = new[] {tempFile.Path};

                Command.Parse(args);
                Assert.AreEqual(0, Command.Execute());
            }
        }
    }
}
