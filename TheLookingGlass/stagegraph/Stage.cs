using System;
using System.Collections.Generic;

namespace TheLookingGlass.StageGraph
{
    internal sealed class Stage<ContentType, SharedContentType>
    {
        internal SharedContentType SharedContent { get; }

        internal string Name { get; }

        internal Dictionary<
            Version<ContentType, SharedContentType>, 
            Scene<ContentType, SharedContentType>> scenes =
            new Dictionary<Version<ContentType, SharedContentType>, Scene<ContentType, SharedContentType>>();

        internal Stage(in ContentType content, in SharedContentType sharedContent, in string name, 
            in Version<ContentType, SharedContentType> baseVersion)
        {
            this.SharedContent = sharedContent;
            this.Name = name;
            scenes.Add(baseVersion, new Scene<ContentType, SharedContentType>(this, content, baseVersion));
        }

        internal void RemoveScene(in Version<ContentType, SharedContentType> version)
        {
            if (version.BaseVersion == null)
            {
                throw ExUtils.RuntimeException("Cannot delete scene at version 0 in stage \"{0}\".", Name);
            }
            if (!scenes.ContainsKey(version))
            {
                throw ExUtils.RuntimeException("No scene at {0} exists in stage \"{1}\".", version, Name);
            }
            scenes.Remove(version);
        }

        internal void AddScene(in Scene<ContentType, SharedContentType> scene)
        {
            if (scenes.ContainsKey(scene.Version))
            {
                throw ExUtils.RuntimeException("{0} already exists in stage \"{1}\".", scene.Version, Name);  
            }
            scenes.Add(scene.Version, scene);
        }

        internal Scene<ContentType, SharedContentType> GetScene(
            Version<ContentType, SharedContentType> version)
        {
            while(version != null)
            {
                if (scenes.ContainsKey(version)) return scenes[version];
                version = version.BaseVersion;
            }
            throw ExUtils.RuntimeException(
                "Versions exhausted looking for {0} in stage \"{1}\".", version, Name);
        }

        internal void ForEachScene(in Action<Scene<ContentType, SharedContentType>> fn)
        {
            foreach (var sceneEntry in scenes) fn(sceneEntry.Value);
        }
    }
}

