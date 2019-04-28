using Microsoft.VisualStudio.TestTools.UnitTesting;
using TheLookingGlass.StageGraph;

namespace TheLookingGlassTests
{
    [TestClass]
    public class StageGraphTest
    {
        [TestMethod]
        public void GraphBuilder_BuildsExpectedGraph_WhenBuiltWithMultipleStages()
        {
            var builder = Graph<string, string>.NewBuilder();
            builder.Add("A", "content_A", "shared_content_A");
            builder.Add("B", "content_B", "shared_content_B");
            builder.Add("C", "content_C", "shared_content_C");

            var graph = builder.Build();

            Assert.AreEqual(graph.versions.Count, 1);
            Assert.AreEqual(graph.stages.Count, 3);
            Assert.AreEqual(graph.frontier.Count, 1);

            var stageA = graph.stages["A"];
            Assert.AreEqual(stageA.scenes.Count, 1);
            Assert.AreEqual(stageA.Name, "A");
            Assert.AreEqual(stageA.SharedContent, "shared_content_A");

            var stageAScene = stageA.scenes[graph.RootVersion];
            Assert.AreEqual(stageAScene.Basis, null);
            Assert.AreEqual(stageAScene.Stage, stageA);
            Assert.AreEqual(stageAScene.Version, graph.RootVersion);
            Assert.AreEqual(stageAScene.Content, "content_A");

            var stageB = graph.stages["B"];
            Assert.AreEqual(stageB.scenes.Count, 1);
            Assert.AreEqual(stageB.Name, "B");
            Assert.AreEqual(stageB.SharedContent, "shared_content_B");

            var stageBScene = stageB.scenes[graph.RootVersion];
            Assert.AreEqual(stageBScene.Basis, null);
            Assert.AreEqual(stageBScene.Stage, stageB);
            Assert.AreEqual(stageBScene.Version, graph.RootVersion);
            Assert.AreEqual(stageBScene.Content, "content_B");

            var stageC = graph.stages["C"];
            Assert.AreEqual(stageC.scenes.Count, 1);
            Assert.AreEqual(stageC.Name, "C");
            Assert.AreEqual(stageC.SharedContent, "shared_content_C");

            var stageCScene = stageC.scenes[graph.RootVersion];
            Assert.AreEqual(stageCScene.Basis, null);
            Assert.AreEqual(stageCScene.Stage, stageC);
            Assert.AreEqual(stageCScene.Version, graph.RootVersion);
            Assert.AreEqual(stageCScene.Content, "content_C");
        }
    }
}
