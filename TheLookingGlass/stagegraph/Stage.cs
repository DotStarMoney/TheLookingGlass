using System;
using System.Collections.Generic;
using TheLookingGlass.Util;

namespace TheLookingGlass.StageGraph
{
    internal sealed class Stage<TContentType, TSharedContentType>
    {
        internal Dictionary<
            Version<TContentType, TSharedContentType>,
            Scene<TContentType, TSharedContentType>> Scenes =
                new Dictionary<
                    Version<TContentType, TSharedContentType>, 
                    Scene<TContentType, TSharedContentType>>();

        internal Stage(in TContentType content, in TSharedContentType sharedContent, in string name,
            in Version<TContentType, TSharedContentType> baseVersion)
        {
            SharedContent = sharedContent;
            Name = name;
            Scenes.Add(baseVersion, new Scene<TContentType, TSharedContentType>(this, content, baseVersion));
        }

        internal TSharedContentType SharedContent { get; }

        internal string Name { get; }

        internal void RemoveScene(in Version<TContentType, TSharedContentType> version)
        {
            if (version.BaseVersion == null)
            {
                throw ExUtils.RuntimeException("Cannot delete scene at version 0 in stage \"{0}\".", Name);
            }

            if (!Scenes.ContainsKey(version)) { 
                throw ExUtils.RuntimeException("No scene at {0} exists in stage \"{1}\".", version, Name);
            }

            Scenes.Remove(version);
        }

        internal void AddScene(in Scene<TContentType, TSharedContentType> scene)
        {
            if (Scenes.ContainsKey(scene.Version))
            {
                throw ExUtils.RuntimeException("{0} already exists in stage \"{1}\".", scene.Version, Name);
            }

            Scenes.Add(scene.Version, scene);
        }

        internal Scene<TContentType, TSharedContentType> GetScene(
            Version<TContentType, TSharedContentType> version)
        {
            while (version != null)
            {
                if (Scenes.ContainsKey(version)) return Scenes[version];
                version = version.BaseVersion;
            }

            throw ExUtils.RuntimeException(
                "Versions exhausted looking for {0} in stage \"{1}\".", version, Name);
        }

        internal void ForEachScene(in Action<Scene<TContentType, TSharedContentType>> fn)
        {
            foreach (var sceneEntry in Scenes) fn(sceneEntry.Value);
        }
    }
}
