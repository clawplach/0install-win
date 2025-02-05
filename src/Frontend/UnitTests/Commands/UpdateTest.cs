/*
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
using Moq;
using NanoByte.Common;
using NanoByte.Common.Storage;
using NUnit.Framework;
using ZeroInstall.Services.Fetchers;
using ZeroInstall.Services.Solvers;
using ZeroInstall.Store.Feeds;
using ZeroInstall.Store.Implementations;
using ZeroInstall.Store.Model;
using ZeroInstall.Store.Model.Selection;

namespace ZeroInstall.Commands
{
    /// <summary>
    /// Contains integration tests for <see cref="Update"/>.
    /// </summary>
    [TestFixture]
    public class UpdateTest : SelectionTestBase<Update>
    {
        [Test(Description = "Ensures local Selections XMLs are correctly detected and parsed.")]
        public override void TestNormal()
        {
            var requirements = RequirementsTest.CreateTestRequirements();
            var selectionsOld = SelectionsTest.CreateTestSelections();
            var selectionsNew = SelectionsTest.CreateTestSelections();
            selectionsNew.Implementations[1].Version = new ImplementationVersion("2.0");
            selectionsNew.Implementations.Add(new ImplementationSelection {InterfaceID = "http://0install.de/feeds/test/sub3.xml", ID = "id3", Version = new ImplementationVersion("0.1")});

            Container.GetMock<ISolver>().SetupSequence(x => x.Solve(requirements)).Returns(selectionsOld).Returns(selectionsNew);

            var impl1 = new Implementation {ID = "id1"};
            var impl2 = new Implementation {ID = "id2"};
            var impl3 = new Implementation {ID = "id3"};
            Container.GetMock<IFeedCache>().Setup(x => x.GetFeed("http://0install.de/feeds/test/sub1.xml")).Returns(new Feed {Uri = new Uri("http://0install.de/feeds/test/sub1.xml"), Elements = {impl1}});
            Container.GetMock<IFeedCache>().Setup(x => x.GetFeed("http://0install.de/feeds/test/sub2.xml")).Returns(new Feed {Uri = new Uri("http://0install.de/feeds/test/sub2.xml"), Elements = {impl2}});
            Container.GetMock<IFeedCache>().Setup(x => x.GetFeed("http://0install.de/feeds/test/sub3.xml")).Returns(new Feed {Uri = new Uri("http://0install.de/feeds/test/sub3.xml"), Elements = {impl3}});

            // Download uncached implementations
            Container.GetMock<IStore>().Setup(x => x.Contains(It.IsAny<ManifestDigest>())).Returns(false);
            Container.GetMock<IFetcher>().Setup(x => x.Fetch(new[] {impl1, impl2, impl3}.IsEquivalent()));

            // Check for <replaced-by>
            Container.GetMock<IFeedCache>().Setup(x => x.GetFeed("http://0install.de/feeds/test/test1.xml")).Returns(FeedTest.CreateTestFeed());

            RunAndAssert("http://0install.de/feeds/test/test2.xml: 1.0 -> 2.0" + Environment.NewLine + "http://0install.de/feeds/test/sub3.xml: new -> 0.1", 0, selectionsNew,
                "http://0install.de/feeds/test/test1.xml", "--command=command", "--os=Windows", "--cpu=i586", "--not-before=1.0", "--before=2.0", "--version-for=http://0install.de/feeds/test/test2.xml", "2.0..!3.0");
        }

        [Test(Description = "Ensures local Selections XMLs are rejected.")]
        public override void TestImportSelections()
        {
            var selections = SelectionsTest.CreateTestSelections();
            using (var tempFile = new TemporaryFile("0install-unit-tests"))
            {
                selections.SaveXml(tempFile);
                Target.Parse(new string[] {tempFile});
                Assert.Throws<NotSupportedException>(() => Target.Execute());
            }
        }
    }
}
