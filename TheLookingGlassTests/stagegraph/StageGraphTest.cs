﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

using TheLookingGlass;
using TheLookingGlass.StageGraph;

namespace TheLookingGlassTests
{
    [TestClass]
    public class StageGraphTest
    {
        [TestMethod]
        public void GraphBuilder_BuildsExpectedGraph_WhenBuiltWithMultipleStages()
        {
            var graph = CreateTestGraph();

            Assert.AreEqual(1, graph.versions.Count);
            Assert.AreEqual(3, graph.stages.Count);
            Assert.AreEqual(1, graph.frontier.Count);

            var stageA = graph.stages["A"];
            Assert.AreEqual(1, stageA.scenes.Count);
            Assert.AreEqual("A", stageA.Name);
            Assert.AreEqual("shared_content_A", stageA.SharedContent);

            var stageAScene = stageA.scenes[graph.RootVersion];
            Assert.AreEqual(null, stageAScene.Basis);
            Assert.AreEqual(stageA, stageAScene.Stage);
            Assert.AreEqual(graph.RootVersion, stageAScene.Version);
            Assert.AreEqual("content_A", stageAScene.Content);

            var stageB = graph.stages["B"];
            Assert.AreEqual(1, stageB.scenes.Count);
            Assert.AreEqual("B", stageB.Name);
            Assert.AreEqual("shared_content_B", stageB.SharedContent);

            var stageBScene = stageB.scenes[graph.RootVersion];
            Assert.AreEqual(null, stageBScene.Basis);
            Assert.AreEqual(stageB, stageBScene.Stage);
            Assert.AreEqual(graph.RootVersion, stageBScene.Version);
            Assert.AreEqual("content_B", stageBScene.Content);

            var stageC = graph.stages["C"];
            Assert.AreEqual(1, stageC.scenes.Count);
            Assert.AreEqual("C", stageC.Name);
            Assert.AreEqual("shared_content_C", stageC.SharedContent);

            var stageCScene = stageC.scenes[graph.RootVersion];
            Assert.AreEqual(null, stageCScene.Basis);
            Assert.AreEqual(stageC, stageCScene.Stage);
            Assert.AreEqual(graph.RootVersion, stageCScene.Version);
            Assert.AreEqual("content_C", stageCScene.Content);
        }

        [TestMethod]
        public void 
            IndexGetContent_ReturnsContentAtDifferentIndex_WhenStageHasNoContentAtCurrentIndexVersion()
        {
            var graph = CreateTestGraph();

            var index = graph.CreateIndex("A");
            Assert.AreEqual("content_A", index.GetContent());

            index.SetContent("content_A_v1");
            Assert.AreEqual("content_A_v1", index.GetContent());

            index.Go("B");
            Assert.AreEqual("content_B", index.GetContent());
        
            Assert.AreEqual(2, graph.versions.Count);
        }

        [TestMethod]
        public void IndexSetContent_DoesntCreateNewVersion_WhenStageHasNoContentAtCurrentIndexVersion()
        {
            var graph = CreateTestGraph();

            var index = graph.CreateIndex("A");
            Assert.AreEqual("content_A", index.GetContent());

            index.SetContent("content_A_v1");
            Assert.AreEqual("content_A_v1", index.GetContent());

            index.Go("B");
            Assert.AreEqual("content_B", index.GetContent());

            index.SetContent("content_B_v1");
            Assert.AreEqual("content_B_v1", index.GetContent());

            Assert.AreEqual(2, graph.versions.Count);
        }

        [TestMethod]
        public void IndexGetContent_ReturnsContentInReverseOrder_WhenSetWithLinkedBases()
        {
            var graph = Graph<string, string>.NewBuilder().Add("A", "content_A", "shared_content_A").Build();

            var index = graph.CreateIndex("A");
            index.SetContent("content_A_v1", true);
            index.SetContent("content_A_v2", true);

            List<string> contentList = new List<string>();
            index.GetContent(content => contentList.Add(content));

            Assert.AreEqual(3, contentList.Count);
            Assert.AreEqual("content_A", contentList[0]);
            Assert.AreEqual("content_A_v1", contentList[1]);
            Assert.AreEqual("content_A_v2", contentList[2]);

            Assert.AreEqual(2, graph.versions.Count);
        }

        [TestMethod]
        public void IndexGetContent_ReturnsSingleContent_WhenSetWithNoLinkedBase()
        {
            var graph = Graph<string, string>.NewBuilder().Add("A", "content_A", "shared_content_A").Build();

            var index = graph.CreateIndex("A");
            index.SetContent("content_A_v1");
            index.SetContent("content_A_v2");

            List<string> contentList = new List<string>();
            index.GetContent(content => contentList.Add(content));

            Assert.AreEqual(1, contentList.Count);
            Assert.AreEqual("content_A_v2", contentList[0]);

            Assert.AreEqual(2, graph.versions.Count);
        }

        [TestMethod]
        public void IndexGetContent_ThrowsInvalidOperationEx_WhenLinkedBaseSetButSingleValueRequested()
        {
            var graph = Graph<string, string>.NewBuilder().Add("A", "content_A", "shared_content_A").Build();

            var index = graph.CreateIndex("A");
            index.SetContent("content_A_v1", true);
            index.SetContent("content_A_v2", true);

            Assert.ThrowsException<InvalidOperationException>(() => index.GetContent());
        }

        [TestMethod]
        public void IndexSetContent_LinksScenes_WhenLinkingBasesFromDifferentVersions()
        {
            var graph = CreateTestGraph();

            var index = graph.CreateIndex("A");
            index.SetContent("content_A_v1", true);

            var unused1 = index.Clone();
            index.SetContent("content_A_v2", true);

            List<string> contentList = new List<string>();
            index.GetContent(content => contentList.Add(content));

            Assert.AreEqual(3, contentList.Count);
            Assert.AreEqual("content_A", contentList[0]);
            Assert.AreEqual("content_A_v1", contentList[1]);
            Assert.AreEqual("content_A_v2", contentList[2]);

            Assert.AreEqual(3, graph.versions.Count);
        }

        [TestMethod]
        public void IndexSetContent_CopiesSceneDescendants_WhenNewSceneAdded()
        {
            var builder = Graph<EmbedToken, string>.NewBuilder();
            builder.Add("A", null, "shared_content_A");
            builder.Add("B", null, "shared_content_B");

            var graph = builder.Build();

            var index = graph.CreateIndex("A");

            index.Go("B");
            var embedMeIndex = index.Clone();

            index.Go("A");
            index.SetContent(
                tokens => Collections.GetOnlyElement(tokens),
                Collections.Of(embedMeIndex));

            Assert.IsFalse(embedMeIndex.IsValid());

            var embedMeIndex2 = index.Clone();

            index.SetContent(null);

            Assert.AreEqual(3, graph.versions.Count);

            Assert.AreEqual(1, index.stage.GetScene(index.version).descendants.Count);
        }

        [TestMethod]
        public void IndexSetContent_RemovesEmbedFromSameVersion_WhenGivenEmbedToken()
        {
            var graph = Graph<EmbedToken, string>.NewBuilder()
                .Add("A", new EmbedToken(-1), "shared_content_A")
                .Build();

            var index = graph.CreateIndex("A");
            Assert.AreEqual(-1, index.GetContent().LookupId);

            index.SetContent(
                tokens => Collections.GetOnlyElement(tokens), 
                Collections.Of(index.Clone()));

            index.SetContent(
                tokens => Collections.GetOnlyElement(tokens),
                Collections.Of(index.Clone()));

            EmbedToken token = index.GetContent();

            index.SetContent(new EmbedToken(-1), Collections.Of(token));

            Assert.ThrowsException<InvalidOperationException>(
                () => index.SetContent(new EmbedToken(-1), Collections.Of(token)),
                String.Format("No element at Id={0} exists.", token.LookupId));
        }
        [TestMethod]
        public void IndexSetContent_RemovesEmbedFromNewVersion_WhenGivenEmbedToken()
        {
            var graph = Graph<EmbedToken, string>.NewBuilder()
                .Add("A", new EmbedToken(-1), "shared_content_A")
                .Build();

            var index = graph.CreateIndex("A");
            Assert.AreEqual(-1, index.GetContent().LookupId);

            index.SetContent(
                tokens => Collections.GetOnlyElement(tokens),
                Collections.Of(index.Clone()));

            index.SetContent(
                tokens => Collections.GetOnlyElement(tokens),
                Collections.Of(index.Clone()));

            EmbedToken token = index.GetContent();

            var tempIndex = index.Clone();
            index.SetContent(new EmbedToken(-1), Collections.Of(token));
            tempIndex.Release();

            Assert.ThrowsException<InvalidOperationException>(
                () => index.SetContent(new EmbedToken(-1), Collections.Of(token)),
                String.Format("No element at Id={0} exists.", token.LookupId));
        }

        [TestMethod]
        public void IndexIndexFromEmbedded_ReconstructsIndex_WhenUnembedded()
        {
            var graph = Graph<EmbedToken, string>.NewBuilder()
                .Add("A", new EmbedToken(-1), "shared_content_A")
                .Build();

            var index = graph.CreateIndex("A");
            Assert.AreEqual(-1, index.GetContent().LookupId);

            var embedMeIndex = index.Clone();
            int tokenId = -1;
            index.SetContent(
                tokens => 
                {
                    EmbedToken token = Collections.GetOnlyElement(tokens);
                    tokenId = token.LookupId;
                    return token;
                },
                Collections.Of(embedMeIndex));

            Assert.IsFalse(embedMeIndex.IsValid());
            Assert.AreEqual(1, index.stage.GetScene(index.version).descendants.Count);

            Assert.AreEqual(tokenId, index.GetContent().LookupId);

            var rescuedIndex = index.IndexFromEmbedded(index.GetContent());
            Assert.AreEqual(-1, rescuedIndex.GetContent().LookupId);

            Assert.AreEqual(1, index.stage.GetScene(index.version).descendants.Count);
            Assert.AreEqual(0, rescuedIndex.stage.GetScene(rescuedIndex.version).descendants.Count);
        }

        [TestMethod]
        public void IndexClone_ProducesIndexThatTracksVersion_WhenClonedFromVersionThatHasSetContent()
        {
            var graph = Graph<string, string>.NewBuilder().Add("A", "content_A", "shared_content_A").Build();

            var index = graph.CreateIndex("A");
            index.SetContent("content_A_v1");

            var olderIndex = index.Clone();

            index.SetContent("content_A_v2");

            Assert.AreEqual("content_A_v1", olderIndex.GetContent());
            Assert.AreEqual("content_A_v2", index.GetContent());
        }

        [TestMethod]
        public void IndexReplace_ReplacesIndexWithNewContent_WhenCalledOnIndexAtOlderVersion()
        {
            var graph = Graph<string, string>.NewBuilder().Add("A", "content_A", "shared_content_A").Build();

            var index = graph.CreateIndex("A");
            index.SetContent("content_A_v1");

            var olderIndex = index.Clone();

            index.SetContent("content_A_v2");

            Assert.AreEqual("content_A_v1", olderIndex.GetContent());
            Assert.AreEqual("content_A_v2", index.GetContent());

            olderIndex.Replace(index);

            Assert.IsFalse(index.IsValid());
            Assert.AreEqual("content_A_v2", olderIndex.GetContent());
        }

        [TestMethod]
        public void GraphCompact_RemovesScene_WhenSceneIsNoLongerAccessible()
        {
            var builder = Graph<string, string>.NewBuilder();
            builder.Add("A", "content_A", "shared_content_A");
            builder.Add("B", "content_B", "shared_content_B");

            var graph = builder.Build();

            var index = graph.CreateIndex("A");
            index.SetContent("content_A_v1");
            index.Go("B");
            index.SetContent("content_B_v1");
            index.Go("A");

            var olderIndex = index.Clone();

            index.SetContent("content_A_v2");

            olderIndex.Release();

            Assert.AreEqual(3, graph.versions.Count);
            Assert.AreEqual(3, graph.GetStage("A").scenes.Count);

            index.Go("A");
            Assert.AreEqual("content_A_v2", index.GetContent());
            index.Go("B");
            Assert.AreEqual("content_B_v1", index.GetContent());

            graph.Compact();

            Assert.AreEqual(3, graph.versions.Count);
            Assert.AreEqual(2, graph.GetStage("A").scenes.Count);

            index.Go("A");
            Assert.AreEqual("content_A_v2", index.GetContent());
            index.Go("B");
            Assert.AreEqual("content_B_v1", index.GetContent());
        }

        [TestMethod]
        public void GraphCompact_RemovesVersion_WhenVersionIsNoLongerAccessible()
        {
            var graph = Graph<string, string>.NewBuilder().Add("A", "content_A", "shared_content_A").Build();

            var index = graph.CreateIndex("A");
            index.SetContent("content_A_v1");

            var olderIndex = index.Clone();

            index.SetContent("content_A_v2");

            index.Release();

            Assert.AreEqual("content_A_v1", olderIndex.GetContent());
            Assert.AreEqual(3, graph.versions.Count);

            graph.Compact();

            Assert.AreEqual("content_A_v1", olderIndex.GetContent());
            Assert.AreEqual(2, graph.versions.Count);
        }

        [TestMethod]
        public void GraphCompact_RemovesVersion_WhenVersionsScenesAreNoLongerAccessible()
        {
            var graph = Graph<string, string>.NewBuilder().Add("A", "content_A", "shared_content_A").Build();

            var index = graph.CreateIndex("A");
            index.SetContent("content_A_v1");

            var olderIndex = index.Clone();

            index.SetContent("content_A_v2");

            olderIndex.Release();

            Assert.AreEqual("content_A_v2", index.GetContent());
            Assert.AreEqual(3, graph.versions.Count);

            graph.Compact();

            Assert.AreEqual("content_A_v2", index.GetContent());
            Assert.AreEqual(2, graph.versions.Count);
        }

        [TestMethod]
        public void GraphCompact_RetainsBaseUnreachableScene_IfReachedByEmbed()
        {
            var builder = Graph<TestContent, string>.NewBuilder();
            builder.Add("A", "content_A", "shared_content_A");

            var graph = builder.Build();

            var tempIndex = graph.CreateIndex("A");
            tempIndex.SetContent("content_A_v1");

            var index = graph.CreateIndex("A");
            index.SetContent(
                tokens => new TestContent("content_A_v2", Collections.GetOnlyElement(tokens)),
                Collections.Of(tempIndex));

            Assert.AreEqual(3, graph.versions.Count);

            graph.Compact();

            Assert.AreEqual(3, graph.versions.Count);
        }

        [TestMethod]
        public void GraphCompact_RemovesScenesAndVersions_WhenGraphHasBothOrphanedScenesAndVersions()
        {
            var graph = createOrphanedSceneAndVersionGraphBuilder(Graph<TestContent, string>.NewBuilder());

            Assert.AreEqual(9, graph.versions.Count);
            Assert.AreEqual(6, graph.stages["A"].scenes.Count);
            Assert.AreEqual(8, graph.stages["B"].scenes.Count);

            graph.Compact();

            Assert.AreEqual(5, graph.versions.Count);
            Assert.AreEqual(4, graph.stages["A"].scenes.Count);
            Assert.AreEqual(4, graph.stages["B"].scenes.Count);
        }

        [TestMethod]
        public void GraphAggressiveCompaction_IsEquivalentToManualCompact_WhenGraphHasBothOrphanedScenesAndVersions()
        {
            var graph = createOrphanedSceneAndVersionGraphBuilder(
                Graph<TestContent, string>.NewBuilder().SetAggressiveCompaction(true));

            Assert.AreEqual(5, graph.versions.Count);
            Assert.AreEqual(4, graph.stages["A"].scenes.Count);
            Assert.AreEqual(4, graph.stages["B"].scenes.Count);
        }

        private static Graph<TestContent, string> createOrphanedSceneAndVersionGraphBuilder(
            in Graph<TestContent, string>.Builder builder)
        {
            builder.Add("A", "content_A", "shared_content_A");
            builder.Add("B", "content_B", "shared_content_B");

            var graph = builder.Build();

            var leftIndex = graph.CreateIndex("A");

            leftIndex.SetContent("content_A_v1");
            leftIndex.Go("B");
            leftIndex.SetContent("content_B_v1");

            var rightIndex = graph.CreateIndex("A");

            rightIndex.SetContent("content_A_v2");
            rightIndex.Go("B");
            rightIndex.SetContent("content_B_v2");

            var tempIndexRight = rightIndex.Clone();
            tempIndexRight.SetContent("content_B_v5");
            tempIndexRight.Release();

            var middleIndex = graph.CreateIndex("B");

            var persistantLeftIndex = leftIndex.Clone();
            EmbedToken token = default;
            middleIndex.SetContent(
                tokens => {
                    token = Collections.GetOnlyElement(tokens);
                    return new TestContent("content_B_v3", token);
                },
                Collections.Of(leftIndex));
            middleIndex.Go("A");
            middleIndex.SetContent("content_A_v3");

            var tempIndex1 = middleIndex.Clone();

            middleIndex.Go("B");
            middleIndex.SetContent(
                tokens => {
                    token = Collections.GetOnlyElement(tokens);
                    return new TestContent("content_B_v4", token);
                },
                Collections.Of(rightIndex),
                Collections.Of(token));

            tempIndex1.Release();

            var leftFork = middleIndex.Clone();
            leftFork.SetContent("content_B_v6", Collections.Of(leftFork.GetContent().Token));
            leftFork.Go("A");
            leftFork.SetContent("content_A_v6");

            var rightFork = middleIndex.Clone();
            rightFork.SetContent("content_B_v7", Collections.Of(rightFork.GetContent().Token));

            middleIndex.Release();

            var tempIndexLeftFork = leftFork.Clone();
            tempIndexLeftFork.SetContent("content_A_v8");
            tempIndexLeftFork.Release();

            return graph;
        }

        private sealed class TestContent
        {
            internal string Text { get; }
            internal EmbedToken Token { get; }

            public static implicit operator TestContent(in string text)
            {
                return new TestContent(text);
            }

            internal TestContent (in string text, in EmbedToken token = null)
            {
                this.Text = text;
                this.Token = token;
            }
        }

        private static Graph<string, string> CreateTestGraph()
        {
            var builder = Graph<string, string>.NewBuilder();
            builder.Add("A", "content_A", "shared_content_A");
            builder.Add("B", "content_B", "shared_content_B");
            builder.Add("C", "content_C", "shared_content_C");

            return builder.Build();
        }
    }
}
