﻿using System;

namespace experimental.StageGraph
{
    internal sealed class Scene<ContentType, SharedContentType>
    {
        internal Stage<ContentType, SharedContentType> Stage { get; }

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

        internal Version<ContentType, SharedContentType> Version { get; }

        internal Scene<ContentType, SharedContentType> Basis { get; }

        private ClaimCheck<Descendant> descendants = new ClaimCheck<Descendant>();
        internal Scene(
            in Stage<ContentType, SharedContentType> owner,
            in Version<ContentType, SharedContentType> version,
            in Scene<ContentType, SharedContentType> basis = null)
        {
            this.Stage = owner;
            this.Version = version;
            this.Basis = basis;
        }

        internal Scene(
            in Stage<ContentType, SharedContentType> owner,
            in ContentType content,
            in Version<ContentType, SharedContentType> version, 
            in Scene<ContentType, SharedContentType> basis = null) : this(owner, version, basis)
        {
            this.content = content;            
        }

        internal void SetContent(in ContentType newContent) => content = newContent;

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
    }
}
