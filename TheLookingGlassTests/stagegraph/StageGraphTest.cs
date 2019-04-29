﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

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

            var index = graph.NewIndex("A");
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

            var index = graph.NewIndex("A");
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

            var index = graph.NewIndex("A");
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

            var index = graph.NewIndex("A");
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

            var index = graph.NewIndex("A");
            index.SetContent("content_A_v1", true);
            index.SetContent("content_A_v2", true);

            Assert.ThrowsException<InvalidOperationException>(() => index.GetContent());
        }

        [TestMethod]
        public void IndexSetContent_LinksScenes_WhenLinkingBasesFromDifferentVersions()
        {
            var graph = CreateTestGraph();

            var index = graph.NewIndex("A");
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

            var index = graph.NewIndex("A");

            index.Go("B");
            var embedMeIndex = index.Clone();

            index.Go("A");
            index.SetContent(
                tokens => {
                    EmbedToken token = null;
                    foreach (var curToken in tokens)
                    {
                        Assert.AreEqual(null, token);
                        token = curToken;
                    }
                    return token;
                }, 
                new List<Index<EmbedToken, string>> { embedMeIndex });

            Assert.IsFalse(embedMeIndex.IsValid());

            var embedMeIndex2 = index.Clone();

            index.SetContent(null);

            Assert.AreEqual(3, graph.versions.Count);

            var indexScene = index.stage.GetScene(index.version);

            Assert.AreEqual(1, indexScene.descendants.Count);
        }

        // Test unembed variants
        // Test clone
        // Test replace
        // Test release

        // Test compact removes a dead version
        // Test compact removes a dead scene

        // Test complex index and compact scenarios


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
