using System;
using System.Collections.Generic;
using System.Linq;
using TheLookingGlass.Util;

namespace TheLookingGlass.StageGraph
{
    public class Index<TContentType, TSharedContentType>
    {
        private Graph<TContentType, TSharedContentType> _graph;

        internal Version<TContentType, TSharedContentType> Version;

        internal Stage<TContentType, TSharedContentType> Stage;

        internal Index(
            in Graph<TContentType, TSharedContentType> graph,
            in Version<TContentType, TSharedContentType> version,
            in Stage<TContentType, TSharedContentType> stage)
        {
            this._graph = graph;
            this.Version = version;
            this.Stage = stage;
        }

        public TSharedContentType GetSharedContent()
        {
            CheckValid();
            return Stage.SharedContent;
        }

        public string GetStage()
        {
            CheckValid();
            return Stage.Name;
        }

        public void GetContent(in Action<TContentType> consumer)
        {
            CheckValid();
            var contentList = new List<TContentType>();
            for (var scene = Stage.GetScene(Version); scene != null; scene = scene.Basis)
            {
                contentList.Add(scene.Content);
            }
            for (var i = contentList.Count - 1; i >= 0; --i) consumer(contentList[i]);
        }

        public TContentType GetContent()
        {
            CheckValid();
            var scene = Stage.GetScene(Version);
            if (scene.Basis != null)
            {
                throw ExUtils.RuntimeException("Content has basis but single content requested.");
            }
            return scene.Content;
        }

        public void Go(in string stageName)
        {
            CheckValid();
            Stage = _graph.GetStage(stageName);
        }

        public void SetContent(
            in Func<IEnumerable<EmbedToken>, TContentType> contentProvider,
            in IEnumerable<Index<TContentType, TSharedContentType>> toEmbed,
            in bool linkBase = false)
        {
            SetContent(contentProvider, toEmbed, Enumerable.Empty<EmbedToken>(), linkBase);
        }

        public void SetContent(
            TContentType content,
            in IEnumerable<EmbedToken> toUnembed,
            in bool linkBase = false)
        {
            SetContent(
                _ => content, 
                Enumerable.Empty<Index<TContentType, TSharedContentType>>(), 
                toUnembed, 
                linkBase);
        }

        public void SetContent(TContentType content, in bool linkBase = false)
        {
            SetContent(_ => content, Enumerable.Empty<Index<TContentType, TSharedContentType>>(), linkBase);
        }

        public void SetContent(
            in Func<IEnumerable<EmbedToken>, TContentType> contentProvider,
            in IEnumerable<Index<TContentType, TSharedContentType>> toEmbed,
            in IEnumerable<EmbedToken> toUnembed,
            in bool linkBase = false)
        {
            CheckValid();
            var scene = Stage.GetScene(Version);

            var updatedVersion = Version;
            if (!Version.Overwritable())
            {
                Version.DecIndexRefs();
                Version.IncParentN();

                updatedVersion = new Version<TContentType, TSharedContentType>(Version);
                _graph.Versions.Add(updatedVersion);
                updatedVersion.AddStage(Stage);
            }
            else
            {
                if (Version == scene.Version)
                {
                    if (linkBase)
                    {
                        var tempScene = new Scene<TContentType, TSharedContentType>(null, null, scene.Basis)
                        {
                            Content = scene.Content
                        };
                        scene.Basis = tempScene;
                    }
                    Unembed(scene, toUnembed);
                    scene.Content = contentProvider(Embed(Version, scene, toEmbed));
                    return;
                }
                updatedVersion.AddStage(Stage);
            }

            var newScene = new Scene<TContentType, TSharedContentType>(
                Stage,
                updatedVersion,
                linkBase ? scene : null);

            scene.ForEachDescendant((target, observedAt) =>
            {
                _ = newScene.AddDescendant(target, observedAt);
                observedAt.IncParentN();
                updatedVersion.IncLinksToEmbeddedVersion(observedAt);
            });
            Unembed(newScene, toUnembed);
            newScene.Content = contentProvider(Embed(updatedVersion, newScene, toEmbed));

            Stage.AddScene(newScene);
            Version = updatedVersion;
        }

        private static void Unembed(
            in Scene<TContentType, TSharedContentType> fromScene,
            in IEnumerable<EmbedToken> toUnembed)
        {
            foreach (var token in toUnembed)
            {
                var descendant = fromScene.GetDescendant(token);

                descendant.ObservedAt.DecParentN();

                fromScene.RemoveDescendant(token);
                fromScene.Version.DecLinksToEmbeddedVersion(descendant.ObservedAt);
            }
        }

        private static IEnumerable<EmbedToken> Embed(
            in Version<TContentType, TSharedContentType> fromVersion,
            in Scene<TContentType, TSharedContentType> fromScene,
            in IEnumerable<Index<TContentType, TSharedContentType>> toEmbed)
        {
            var tokens = new List<EmbedToken>();
            foreach (var embedIndex in toEmbed)
            {
                var embedVersion = embedIndex.Version;
                embedVersion.DecIndexRefs();
                embedVersion.IncParentN();

                fromVersion.IncLinksToEmbeddedVersion(embedVersion);
                tokens.Add(fromScene.AddDescendant(embedIndex.Stage.GetScene(embedVersion), embedVersion));

                embedIndex.Invalidate();
            }
            return tokens;
        }

        public Index<TContentType, TSharedContentType> IndexFromEmbedded(in EmbedToken token)
        {
            CheckValid();
            var scene = Stage.GetScene(Version);

            var descendant = scene.GetDescendant(token);
            descendant.ObservedAt.IncIndexRefs();

            return new Index<TContentType, TSharedContentType>(
                _graph,
                descendant.ObservedAt,
                descendant.Target.Stage);
        }

        public Index<TContentType, TSharedContentType> Clone()
        {
            CheckValid();
            Version.IncIndexRefs();
            return new Index<TContentType, TSharedContentType>(_graph, Version, Stage);
        }

        public void Replace(in Index<TContentType, TSharedContentType> index)
        {
            Release();
            _graph = index._graph;
            Version = index.Version;
            Stage = index.Stage;
            index.Invalidate();

            _graph.MaybeCompact();
        }

        public void Release()
        {
            if (!IsValid()) return;
            Version.DecIndexRefs();

            _graph.MaybeCompact();
            Invalidate();
        }

        public bool IsValid() => _graph != null;
            
        private void CheckValid()
        {
            if (!IsValid()) throw ExUtils.RuntimeException("Invalid Index.");
        }

        private void Invalidate()
        {
            _graph = null;
            Version = null;
            Stage = null;
        }
    }
}
