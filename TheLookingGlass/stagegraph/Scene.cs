using System;

namespace experimental.StageGraph
{
    internal sealed class Scene<ContentType, SharedContentType>
    {
        internal Stage<ContentType, SharedContentType> Owner { get; }

        internal ContentType Content { get; }

        internal Version<ContentType, SharedContentType> Version { get; }

        internal Scene<ContentType, SharedContentType> Basis { get; }

        private ClaimCheck<Scene<ContentType, SharedContentType>> descendants = 
            new ClaimCheck<Scene<ContentType, SharedContentType>>();

        internal Scene(in Stage<ContentType, SharedContentType> owner, in ContentType content,
            in Version<ContentType, SharedContentType> version, 
            in Scene<ContentType, SharedContentType> basis = null)
        {
            this.Owner = owner;
            this.Content = content;
            this.Version = version;
            this.Basis = basis;
        }

        internal void ForEachDescendant(in Action<Scene<ContentType, SharedContentType>> fn)
        {
            foreach (var descendant in descendants) fn(obj: descendant);
        }

        internal EmbedToken AddDescendant(in Scene<ContentType, SharedContentType> descendant)
        {
            return new EmbedToken(descendants.Add(descendant));
        }

        internal void RemoveDescendant(in EmbedToken token)
        {
            descendants.Remove(token.LookupId);
        }
    }
}
