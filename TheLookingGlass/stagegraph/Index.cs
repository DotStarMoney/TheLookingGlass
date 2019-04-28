using System;
using System.Collections.Generic;
using System.Linq;

namespace TheLookingGlass.StageGraph
{
    public class Index<ContentType, SharedContentType>
    {
        private Graph<ContentType, SharedContentType> graph;

        private Version<ContentType, SharedContentType> version;

        private Stage<ContentType, SharedContentType> stage;

        internal Index(
            in Graph<ContentType, SharedContentType> graph,
            in Version<ContentType, SharedContentType> version,
            in Stage<ContentType, SharedContentType> stage)
        {
            this.graph = graph;
            this.version = version;
            this.stage = stage;
        }

        public SharedContentType GetSharedContent()
        {
            CheckValid();
            return stage.SharedContent;
        }

        public string GetStage()
        {
            CheckValid();
            return stage.Name;
        }

        public void GetContent(in Action<ContentType> consumer)
        {
            CheckValid();
            List<ContentType> contentList = new List<ContentType>();
            for (var scene = stage.GetScene(version); scene != null; scene = scene.Basis)
            {
                contentList.Add(scene.Content);
            }
            for (int i = contentList.Count - 1; i >= 0; --i) consumer(obj: contentList[i]);
        }

        public ContentType GetContent()
        {
            CheckValid();
            var scene = stage.GetScene(version);
            if (scene.Basis != null)
            {
                throw ExUtils.RuntimeException("Content has basis but single content requested.");
            }
            return scene.Content;
        }

        public void Go(in string stageName)
        {
            CheckValid();
            stage = graph.GetStage(stageName);
        }

        public void SetContent(
            in Func<IEnumerable<EmbedToken>, ContentType> contentProvider,
            in IEnumerable<Index<ContentType, SharedContentType>> toEmbed,
            in bool linkBase = false)
        {
            CheckValid();
            var scene = stage.GetScene(version);

            if (version.Overwritable() && (version == scene.Version))
            {
                scene.SetContent(contentProvider(EmbedInDescendants(version, scene, toEmbed)));
                return;
            }

            var updatedVersion = version;
            if (!version.Overwritable())
            {
                version.DecIndexRefs();
                version.IncParentN();

                updatedVersion = new Version<ContentType, SharedContentType>(version);
                graph.versions.Add(updatedVersion);
                updatedVersion.AddStage(stage);

                if (graph.frontier.Remove(version))
                {
                    graph.frontier.Add(updatedVersion);
                }
            }
            else
            {
                updatedVersion.AddStage(stage);
            }

            var newScene = new Scene<ContentType, SharedContentType>(
                stage,
                updatedVersion,
                linkBase ? scene.Basis : null);

            scene.ForEachDescendant((target, observedAt) =>
            {
                _ = newScene.AddDescendant(target, observedAt);
                observedAt.IncParentN();
                updatedVersion.IncLinksToEmbeddedVersion(observedAt);
            });
            newScene.Content = contentProvider(EmbedInDescendants(updatedVersion, newScene, toEmbed));

            stage.AddScene(newScene);
            version = updatedVersion;
        }

        private static IEnumerable<EmbedToken> EmbedInDescendants(
            in Version<ContentType, SharedContentType> fromVersion,
            in Scene<ContentType, SharedContentType> fromScene,
            in IEnumerable<Index<ContentType, SharedContentType>> toEmbed)
        {
            List<EmbedToken> tokens = new List<EmbedToken>();
            foreach (var embedIndex in toEmbed)
            {
                var embedVersion = embedIndex.version;
                embedVersion.DecIndexRefs();
                embedVersion.IncParentN();

                fromVersion.IncLinksToEmbeddedVersion(embedVersion);
                tokens.Add(fromScene.AddDescendant(embedIndex.stage.GetScene(embedVersion), embedVersion));

                embedIndex.Invalidate();
            }
            return tokens;
        }

        public void SetContent(ContentType content, in bool linkBase = false)
        {
            SetContent(_ => content, Enumerable.Empty<Index<ContentType, SharedContentType>>(), linkBase);
        }

        public Index<ContentType, SharedContentType> Unembed(in EmbedToken token)
        {
            CheckValid();
            var scene = stage.GetScene(version);

            var descendant = scene.RemoveDescendant(token);

            descendant.ObservedAt.IncIndexRefs();
            descendant.ObservedAt.DecParentN();
            version.DecLinksToEmbeddedVersion(descendant.ObservedAt);

            return new Index<ContentType, SharedContentType>(
                graph,
                descendant.ObservedAt,
                descendant.Target.Stage);
        }

        public Index<ContentType, SharedContentType> IndexFromEmbedded(in EmbedToken token)
        {
            CheckValid();
            var scene = stage.GetScene(version);

            var descendant = scene.RemoveDescendant(token);
            descendant.ObservedAt.IncIndexRefs();

            return new Index<ContentType, SharedContentType>(
                graph,
                descendant.ObservedAt,
                descendant.Target.Stage);
        }

        public Index<ContentType, SharedContentType> Clone()
        {
            CheckValid();
            version.IncIndexRefs();
            return new Index<ContentType, SharedContentType>(graph, version, stage);
        }

        public void Replace(in Index<ContentType, SharedContentType> index)
        {
            Release();
            graph = index.graph;
            version = index.version;
            stage = index.stage;
            index.Invalidate();
        }

        public void Release()
        {
            if (!IsValid()) return;
            CheckValid();
            Invalidate();

            version.DecIndexRefs();

            graph.Compact();
        }

        public bool IsValid() => graph != null;
            
        private void CheckValid()
        {
            if (!IsValid())
            {
                throw ExUtils.RuntimeException("Invalid Index.");
            }
        }

        private void Invalidate()
        {
            graph = null;
            version = null;
            stage = null;
        }
    }
}
