using System;
using TheLookingGlass.Util;

namespace TheLookingGlass.StageGraph
{
    internal sealed class Scene<TContentType, TSharedContentType>
    {
        internal Stage<TContentType, TSharedContentType> Stage { get; private set; }

        private TContentType _content;

        internal TContentType Content
        {
            get
            {
                if (_content == null)
                {
                    throw ExUtils.RuntimeException("Content not set.");
                }
                return _content;
            }
            set => _content = value;
        }

        internal Version<TContentType, TSharedContentType> Version { get; private set; }

        internal Scene<TContentType, TSharedContentType> Basis { get; set; }

        internal ClaimCheck<Descendant> Descendants = new ClaimCheck<Descendant>();

        internal Scene(
            in Stage<TContentType, TSharedContentType> owner,
            in Version<TContentType, TSharedContentType> version,
            in Scene<TContentType, TSharedContentType> basis = null)
        {
            this.Stage = owner;
            this.Version = version;
            this.Basis = basis;
        }

        internal Scene(
            in Stage<TContentType, TSharedContentType> owner,
            in TContentType content,
            in Version<TContentType, TSharedContentType> version, 
            in Scene<TContentType, TSharedContentType> basis = null) : this(owner, version, basis)
        {
            this._content = content;            
        }

        internal void ForEachDescendant(
            in Action<Scene<TContentType, TSharedContentType>, Version<TContentType, TSharedContentType>> fn)
        {
            foreach (var descendant in Descendants) fn(descendant.Target, descendant.ObservedAt);
        }

        internal EmbedToken AddDescendant(
            in Scene<TContentType, TSharedContentType> target, 
            in Version<TContentType, TSharedContentType> observedAt)
        {
            return new EmbedToken(Descendants.Add(new Descendant(target, observedAt)));
        }

        internal Descendant RemoveDescendant(in EmbedToken token) => Descendants.Remove(token.LookupId);

        internal Descendant GetDescendant(in EmbedToken token) => Descendants.Get(token.LookupId);

        internal sealed class Descendant
        {
            internal Scene<TContentType, TSharedContentType> Target { get; }
            internal Version<TContentType, TSharedContentType> ObservedAt { get; }

            internal Descendant(
                in Scene<TContentType, TSharedContentType> target,
                in Version<TContentType, TSharedContentType> observedAt)
            {
                this.Target = target;
                this.ObservedAt = observedAt;
            }
        }

        internal void ClearForGc()
        {
            Stage = null;
            Version = null;
        }
    }
}
