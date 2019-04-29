using System;

namespace TheLookingGlass.StageGraph
{
    internal sealed class Scene<ContentType, SharedContentType>
    {
        internal Stage<ContentType, SharedContentType> stage;

        internal Stage<ContentType, SharedContentType> Stage { get => stage; }

        private ContentType content;

        internal ContentType Content
        {
            get
            {
                if (content == null)
                {
                    throw ExUtils.RuntimeException("Content not set.");
                }
                return content;
            }
            set => content = value;
        }

        private Version<ContentType, SharedContentType> version;

        internal Version<ContentType, SharedContentType> Version { get => version; }

        internal Scene<ContentType, SharedContentType> basis;

        internal Scene<ContentType, SharedContentType> Basis { get => basis; }

        internal ClaimCheck<Descendant> descendants = new ClaimCheck<Descendant>();
        internal Scene(
            in Stage<ContentType, SharedContentType> owner,
            in Version<ContentType, SharedContentType> version,
            in Scene<ContentType, SharedContentType> basis = null)
        {
            this.stage = owner;
            this.version = version;
            this.basis = basis;
        }

        internal Scene(
            in Stage<ContentType, SharedContentType> owner,
            in ContentType content,
            in Version<ContentType, SharedContentType> version, 
            in Scene<ContentType, SharedContentType> basis = null) : this(owner, version, basis)
        {
            this.content = content;            
        }

        internal void ForEachDescendant(
            in Action<Scene<ContentType, SharedContentType>, Version<ContentType, SharedContentType>> fn)
        {
            foreach (var descendant in descendants) fn(descendant.Target, descendant.ObservedAt);
        }

        internal EmbedToken AddDescendant(
            in Scene<ContentType, SharedContentType> target, 
            in Version<ContentType, SharedContentType> observedAt)
        {
            return new EmbedToken(descendants.Add(new Descendant(target, observedAt)));
        }

        internal Descendant RemoveDescendant(in EmbedToken token) => descendants.Remove(token.LookupId);

        internal Descendant GetDescendant(in EmbedToken token) => descendants.Get(token.LookupId);

        internal sealed class Descendant
        {
            internal Scene<ContentType, SharedContentType> Target { get; }
            internal Version<ContentType, SharedContentType> ObservedAt { get; }

            internal Descendant(
                in Scene<ContentType, SharedContentType> target,
                in Version<ContentType, SharedContentType> observedAt)
            {
                this.Target = target;
                this.ObservedAt = observedAt;
            }
        }

        internal void ClearForGc()
        {
            stage = null;
            version = null;
        }
    }
}
