using System;
using System.Collections.Generic;
using TheLookingGlass.Util;

namespace TheLookingGlass.StageGraph
{
    internal sealed class Version<TContentType, TSharedContentType>
    {
        private readonly Dictionary<Version<TContentType, TSharedContentType>, uint> _embeddingLinksN =
            new Dictionary<Version<TContentType, TSharedContentType>, uint>();

        private uint _indexRefN = 1;
        private uint _parentN;

        internal Version(in Version<TContentType, TSharedContentType> baseVersion = null)
        {
            BaseVersion = baseVersion;
        }

        internal HashSet<Stage<TContentType, TSharedContentType>> InStages { get; } =
            new HashSet<Stage<TContentType, TSharedContentType>>();

        internal Version<TContentType, TSharedContentType> BaseVersion { get; set; }

        internal void ForEachUniqueNonRootLink(in Action<Version<TContentType, TSharedContentType>> fn)
        {
            if (IsRoot()) return;
            foreach (var entry in _embeddingLinksN)
            {
                if (!entry.Key.IsRoot() && entry.Key != BaseVersion) fn(entry.Key);
            }

            fn(BaseVersion);
        }

        internal void IncIndexRefs()
        {
            _indexRefN++;
        }

        internal void DecIndexRefs()
        {
            if (_indexRefN == 0)
            {
                throw ExUtils.RuntimeException("Index reference count underflow in {0}.", this);
            }
            --_indexRefN;
        }

        internal bool ReferencedByIndex()
        {
            return _indexRefN != 0;
        }

        internal bool Overwritable()
        {
            if (_indexRefN == 0)
            {
                throw ExUtils.RuntimeException("Version should only be tested for overwrite when "
                                               + "referenced at least once in {0}.", this);
            }

            return _indexRefN == 1 && HasNoParents();
        }

        internal void AddStage(in Stage<TContentType, TSharedContentType> stage)
        {
            InStages.Add(stage);
        }

        internal void RemoveStage(in Stage<TContentType, TSharedContentType> stage)
        {
            InStages.Remove(stage);
        }

        internal void IncLinksToEmbeddedVersion(in Version<TContentType, TSharedContentType> linkVersion)
        {
            if (!_embeddingLinksN.ContainsKey(linkVersion))
            {
                _embeddingLinksN.Add(linkVersion, 1);
                return;
            }

            _embeddingLinksN[linkVersion]++;
        }

        internal void DecLinksToEmbeddedVersion(in Version<TContentType, TSharedContentType> linkVersion)
        {
            if (!_embeddingLinksN.ContainsKey(linkVersion))
            {
                throw ExUtils.RuntimeException("Linked embedded version {0} not present in {1}.",
                    linkVersion, this);
            }

            if (--_embeddingLinksN[linkVersion] == 0) _ = _embeddingLinksN.Remove(linkVersion);
        }

        internal void IncParentN()
        {
            ++_parentN;
        }

        internal void DecParentN()
        {
            if (_parentN == 0) throw ExUtils.RuntimeException("Parent count underflow in {0}.", this);
            --_parentN;
        }

        internal bool HasNoParents()
        {
            return _parentN == 0;
        }

        internal bool IsRoot()
        {
            return BaseVersion == null;
        }

        public override string ToString()
        {
            return $"Version<{GetHashCode()}>";
        }
    }
}
