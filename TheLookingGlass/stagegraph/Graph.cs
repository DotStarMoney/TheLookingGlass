using System;
using System.Collections.Generic;
using System.Linq;
using TheLookingGlass.Util;

namespace TheLookingGlass.StageGraph
{
    public sealed class Graph<TContentType, TSharedContentType>
    {
        private readonly bool _aggressiveCompaction;


        internal Dictionary<string, Stage<TContentType, TSharedContentType>> Stages =
            new Dictionary<string, Stage<TContentType, TSharedContentType>>();


        internal HashSet<Version<TContentType, TSharedContentType>> Versions =
            new HashSet<Version<TContentType, TSharedContentType>>();

        private Graph(in Builder builder)
        {
            RootVersion = new Version<TContentType, TSharedContentType>();
            Versions.Add(RootVersion);

            foreach (var fragment in builder.Fragments)
            {
                var stage = new Stage<TContentType, TSharedContentType>(
                    fragment.Content,
                    fragment.SharedContent,
                    fragment.Name,
                    RootVersion);
                Stages.Add(fragment.Name, stage);
                RootVersion.AddStage(stage);
            }

            _aggressiveCompaction = builder.AggressiveCompaction;
        }

        internal Version<TContentType, TSharedContentType> RootVersion { get; }

        internal Stage<TContentType, TSharedContentType> GetStage(in string name)
        {
            if (Stages.ContainsKey(name)) return Stages[name];
            throw ExUtils.RuntimeException("Stage \"{0}\" not found.", name);
        }

        public void Compact()
        {
            var orphanedScenes = IsolateOrphanedScenes();

            var emptyVersions =
                new List<Version<TContentType, TSharedContentType>>();
            foreach (var scene in orphanedScenes)
            {
                var sceneVersion = scene.Version;
                PurgeScene(scene);
                if (!sceneVersion.InStages.Any()) emptyVersions.Add(sceneVersion);
            }

            foreach (var version in emptyVersions)
            {
                version.ForEachUniqueNonRootLink(linkedVersion =>
                {
                    if (!Versions.Contains(linkedVersion)) return;
                    linkedVersion.DecParentN();
                });
                Versions.Remove(version);
            }
        }

        private static void PurgeScene(in Scene<TContentType, TSharedContentType> scene)
        {
            var sceneVersion = scene.Version;
            scene.ForEachDescendant((_, descendantVersion) =>
            {
                sceneVersion.DecLinksToEmbeddedVersion(descendantVersion);
                descendantVersion.DecParentN();
            });
            sceneVersion.RemoveStage(scene.Stage);
            scene.Stage.RemoveScene(sceneVersion);
            scene.ClearForGc();
        }

        private IEnumerable<Scene<TContentType, TSharedContentType>> IsolateOrphanedScenes()
        {
            var exploreVersions = new Stack<CompactStageMask>();
            foreach (var version in Versions)
            {
                if (version.ReferencedByIndex())
                {
                    exploreVersions.Push(new CompactStageMask(version, GetAllStages()));
                }
            }

            var orphanedScenes = GetNonRootScenes();
            while (exploreVersions.Any())
            {
                var exploreVersion = exploreVersions.Pop();

                var deadScenesLength = orphanedScenes.Count;
                ForEachSceneAtVersion(exploreVersion.Version, scene =>
                {
                    if (exploreVersion.Mask.Contains(scene.Stage) && orphanedScenes.Remove(scene))
                        scene.ForEachDescendant((descendant, _) =>
                        {
                            exploreVersions.Push(
                                new CompactStageMask(descendant.Version, GetAllStages()));
                        });
                });
                if (deadScenesLength == orphanedScenes.Count) continue;

                var mask = exploreVersion.Mask;
                mask.ExceptWith(exploreVersion.Version.InStages);

                var reassignedBase = false;
                if (mask.Any())
                    for (
                        var version = exploreVersion.Version.BaseVersion;
                        version != RootVersion;
                        version = version.BaseVersion)
                    {
                        foreach (var stage in version.InStages)
                        {
                            if (!mask.Contains(stage)) continue;
                            exploreVersions.Push(new CompactStageMask(
                                version,
                                new HashSet<Stage<TContentType, TSharedContentType>>(mask)));
                            ReassignVersionBase(exploreVersion.Version, version);
                            reassignedBase = true;
                            break;
                        }

                        if (reassignedBase) break;
                    }

                if (!reassignedBase) ReassignVersionBase(exploreVersion.Version, RootVersion);
            }

            return orphanedScenes;
        }

        private static void ReassignVersionBase(
            in Version<TContentType, TSharedContentType> source,
            in Version<TContentType, TSharedContentType> newBase)
        {
            source.BaseVersion.DecParentN();
            source.BaseVersion = newBase;
            newBase.IncParentN();
        }

        private static void ForEachSceneAtVersion(
            in Version<TContentType, TSharedContentType> version,
            in Action<Scene<TContentType, TSharedContentType>> fn)
        {
            foreach (var stage in version.InStages) fn(stage.GetScene(version));
        }

        private HashSet<Stage<TContentType, TSharedContentType>> GetAllStages()
        {
            return new HashSet<Stage<TContentType, TSharedContentType>>(Stages.Values);
        }

        private HashSet<Scene<TContentType, TSharedContentType>> GetNonRootScenes()
        {
            var allScenes = new HashSet<Scene<TContentType, TSharedContentType>>();
            foreach (var stageEntry in Stages)
            {
                stageEntry.Value.ForEachScene(scene =>
                {
                    if (scene.Version == RootVersion) return;
                    allScenes.Add(scene);
                });
            }

            return allScenes;
        }

        public Index<TContentType, TSharedContentType> CreateIndex(in string stageName)
        {
            lock (RootVersion)
            {
                RootVersion.IncIndexRefs();
            }

            return new Index<TContentType, TSharedContentType>(this, RootVersion, GetStage(stageName));
        }

        public static Builder NewBuilder() => new Builder();

        internal void MaybeCompact()
        {
            if (_aggressiveCompaction) Compact();
        }

        private sealed class CompactStageMask
        {
            internal CompactStageMask(
                in Version<TContentType, TSharedContentType> version,
                in HashSet<Stage<TContentType, TSharedContentType>> mask)
            {
                Version = version;
                Mask = mask;
            }

            internal Version<TContentType, TSharedContentType> Version { get; }

            internal HashSet<Stage<TContentType, TSharedContentType>> Mask { get; }
        }

        public sealed class Builder
        {
            internal bool AggressiveCompaction;

            internal List<StageFragment> Fragments { get; } = new List<StageFragment>();

            public Builder Add(in string name, in TContentType content, in TSharedContentType sharedContent)
            {
                Fragments.Add(new StageFragment(name, content, sharedContent));
                return this;
            }

            public Builder SetAggressiveCompaction(in bool aggressiveCompaction)
            {
                AggressiveCompaction = aggressiveCompaction;
                return this;
            }

            public void Clear()
            {
                Fragments.Clear();
                AggressiveCompaction = false;
            }

            public Graph<TContentType, TSharedContentType> Build()
            {
                return new Graph<TContentType, TSharedContentType>(this);
            }

            internal sealed class StageFragment
            {
                internal StageFragment(in string name, in TContentType content,
                    in TSharedContentType sharedContent)
                {
                    Name = name;
                    Content = content;
                    SharedContent = sharedContent;
                }

                internal string Name { get; }

                internal TContentType Content { get; }

                internal TSharedContentType SharedContent { get; }
            }
        }
    }
}
