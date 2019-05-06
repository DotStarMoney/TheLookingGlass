using System;
using System.Collections.Generic;
using System.Linq;

namespace TheLookingGlass.StageGraph
{
    public sealed class Graph<ContentType, SharedContentType>
    {
        private readonly bool aggressiveCompaction;

        internal Dictionary<string, Stage<ContentType, SharedContentType>> stages =
            new Dictionary<string, Stage<ContentType, SharedContentType>>();

        internal HashSet<Version<ContentType, SharedContentType>> versions =
            new HashSet<Version<ContentType, SharedContentType>>();

        internal HashSet<Version<ContentType, SharedContentType>> frontier = 
            new HashSet<Version<ContentType, SharedContentType>>();

        internal Version<ContentType, SharedContentType> RootVersion { get; }

        internal Stage<ContentType, SharedContentType> GetStage(in string name)
        {
            if (stages.ContainsKey(name)) return stages[name];
            throw ExUtils.RuntimeException("Stage \"{0}\" not found.", name);
        }

        public void Compact()
        {
            var orphanedScenes = IsolateOrphanedScenes();

            List<Version<ContentType, SharedContentType>> emptyVersions =
                new List<Version<ContentType, SharedContentType>>();
            foreach (var scene in orphanedScenes)
            {
                var sceneVersion = scene.Version;
                PurgeScene(scene);
                if (!sceneVersion.InStages.Any())
                {
                    emptyVersions.Add(sceneVersion);
                }
            }

            foreach (var version in emptyVersions)
            {
                version.ForEachUniqueNonRootLink(linkedVersion =>
                {
                    if (!versions.Contains(linkedVersion)) return;
                    linkedVersion.DecParentN();
                });
                versions.Remove(version);
            }
        }

        private void PurgeScene(in Scene<ContentType, SharedContentType> scene)
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

        private HashSet<Scene<ContentType, SharedContentType>> IsolateOrphanedScenes()
        {
            var exploreVersions = new Stack<CompactStageMask>();
            foreach(var version in versions)
            {
                if (version.ReferencedByIndex())
                {
                    exploreVersions.Push(new CompactStageMask(version, GetAllStages()));
                }
            }

            var orphanedScenes = GetNonRootScenes();
            while(exploreVersions.Any())
            {
                var exploreVersion = exploreVersions.Pop();

                int deadScenesLength = orphanedScenes.Count;
                ForEachSceneAtVersion(exploreVersion.Version, scene =>
                {
                    if (exploreVersion.Mask.Contains(scene.Stage) && orphanedScenes.Remove(scene))
                    {
                        scene.ForEachDescendant((descendant, _) =>
                        {
                            exploreVersions.Push(new CompactStageMask(descendant.Version, GetAllStages()));
                        });
                    }
                });
                if (deadScenesLength == orphanedScenes.Count) continue;

                var mask = exploreVersion.Mask;
                mask.ExceptWith(exploreVersion.Version.InStages);

                bool reassignedBase = false;
                if (mask.Any())
                {
                    for (
                        var version = exploreVersion.Version.BaseVersion;
                        version != RootVersion;
                        version = version.BaseVersion)
                    {
                        foreach (var stage in version.InStages)
                        {
                            if (mask.Contains(stage))
                            {
                                exploreVersions.Push(new CompactStageMask(
                                    version,
                                    new HashSet<Stage<ContentType, SharedContentType>>(mask)));
                                ReassignVersionBase(exploreVersion.Version, version);
                                reassignedBase = true;
                                break;
                            }
                        }
                        if (reassignedBase) break;
                    }
                }
                if (!reassignedBase) ReassignVersionBase(exploreVersion.Version, RootVersion);
            }
            return orphanedScenes;
        }

        private static void ReassignVersionBase(
            in Version<ContentType, SharedContentType> source,
            in Version<ContentType, SharedContentType> newBase)
        {
            source.BaseVersion.DecParentN();
            source.BaseVersion = newBase;
            newBase.IncParentN();
        }

        private void ForEachSceneAtVersion(
            in Version<ContentType, SharedContentType> version, 
            in Action<Scene<ContentType, SharedContentType>> fn)
        {
            foreach(var stage in version.InStages) fn(stage.GetScene(version));
        }

        private HashSet<Stage<ContentType, SharedContentType>> GetAllStages()
        {
            return new HashSet<Stage<ContentType, SharedContentType>>(stages.Values);
        }

        private HashSet<Scene<ContentType, SharedContentType>> GetNonRootScenes()
        {
            var allScenes = new HashSet<Scene<ContentType, SharedContentType>>();
            foreach (var stageEntry in stages)
            {
                stageEntry.Value.ForEachScene(scene =>
                {
                    if (scene.Version == RootVersion) return;
                    allScenes.Add(scene);
                });
            }
            return allScenes;
        }

        private sealed class CompactBranch
        {
            internal Version<ContentType, SharedContentType> Version { get; }

            internal bool Accessible { get; }

            internal CompactBranch(in Version<ContentType, SharedContentType> version, in bool accessible)
            {
                this.Version = version;
                this.Accessible = accessible;
            }
        }

        private sealed class CompactStageMask
        {
            internal Version<ContentType, SharedContentType> Version { get; }

            internal HashSet<Stage<ContentType, SharedContentType>> Mask { get; }
            
            internal CompactStageMask(
                in Version<ContentType, SharedContentType> version, 
                in HashSet<Stage<ContentType, SharedContentType>> mask)
            {
                this.Version = version;
                this.Mask = mask;
            }
        }

        public Index<ContentType, SharedContentType> CreateIndex(in string stageName)
        {
            lock(RootVersion)
            {
                RootVersion.IncIndexRefs();
            }
            return new Index<ContentType, SharedContentType>(this, RootVersion, GetStage(stageName));
        }

        public static Builder NewBuilder() => new Builder();

        public sealed class Builder
        {
            private List<StageFragment> fragments = new List<StageFragment>();

            internal List<StageFragment> Fragments { get { return fragments; } }

            internal bool aggressiveCompaction = false;

            internal Builder() { }

            public Builder Add(in string name, in ContentType content, in SharedContentType sharedContent)
            {
                fragments.Add(new StageFragment(name, content, sharedContent));
                return this;
            }

            public Builder SetAggressiveCompaction(in bool aggressiveCompaction)
            {
                this.aggressiveCompaction = aggressiveCompaction;
                return this;
            }

            public void Clear()
            {
                fragments.Clear();
                aggressiveCompaction = false;
            }

            public Graph<ContentType, SharedContentType> Build()
            {
                return new Graph<ContentType, SharedContentType>(this);
            }

            internal sealed class StageFragment
            {
                internal string Name { get; }
                
                internal ContentType Content { get; }

                internal SharedContentType SharedContent { get; }

                internal StageFragment(in string name, in ContentType content, 
                    in SharedContentType sharedContent)
                {
                    this.Name = name;
                    this.Content = content;
                    this.SharedContent = sharedContent;
                }
            }
        }

        internal void maybeCompact()
        {
            if (aggressiveCompaction) Compact();
        }

        private Graph(in Builder builder)
        {
            this.RootVersion = new Version<ContentType, SharedContentType>();
            this.versions.Add(this.RootVersion);
            this.frontier.Add(this.RootVersion);

            foreach(var fragment in builder.Fragments)
            {
                var stage = new Stage<ContentType, SharedContentType>(
                    fragment.Content,
                    fragment.SharedContent,
                    fragment.Name,
                    this.RootVersion);
                this.stages.Add(fragment.Name, stage);
                this.RootVersion.AddStage(stage);
            }

            this.aggressiveCompaction = builder.aggressiveCompaction;
        }
    }
}
