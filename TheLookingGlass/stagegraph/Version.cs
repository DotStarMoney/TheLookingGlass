using System;
using System.Collections.Generic;

namespace experimental.StageGraph
{
    internal sealed class Version<ContentType, SharedContentType>
    {
        private uint parentN = 0;

        private uint allRefN = 1;

        private uint indexRefN = 1;

        private Dictionary<Version<ContentType, SharedContentType>, uint> embeddingLinksN = 
            new Dictionary<Version<ContentType, SharedContentType>, uint>();

        private HashSet<Stage<ContentType, SharedContentType>> inStages = 
            new HashSet<Stage<ContentType, SharedContentType>>();

        internal HashSet<Stage<ContentType, SharedContentType>> InStages { get { return inStages; } }

        internal Version<ContentType, SharedContentType> BaseVersion { get; set; }

        internal Version(in Version<ContentType, SharedContentType> baseVersion = null)
        {
            this.BaseVersion = baseVersion;
        }

        internal void ForEachEmbeddedLink(in Action<Version<ContentType, SharedContentType>> fn)
        {
            if (IsRoot()) return;
            foreach (KeyValuePair<Version<ContentType, SharedContentType>, uint> entry in embeddingLinksN)
            {
                if (!entry.Key.IsRoot()) fn(obj: entry.Key);
            }
        }

        internal void ForEachUniqueNonRootLink(in Action<Version<ContentType, SharedContentType>> fn)
        {
            if (IsRoot()) return;
            foreach (KeyValuePair<Version<ContentType, SharedContentType>, uint> entry in embeddingLinksN)
            {
                if (!entry.Key.IsRoot() && (entry.Key != BaseVersion)) fn(obj: entry.Key);
            }
            fn(obj: BaseVersion);
        }

        internal void IncAllRefs() => allRefN++;
        internal void DecAllRefs()
        {
            if (allRefN == 0)
            {
                throw ExUtils.RuntimeException("Total reference count underflow in {0}.", this);
            }
            --allRefN;
        }
        internal void IncIndexRefs() => indexRefN++;
        internal void DecIndexRefs()
        {
            if (indexRefN == 0)
            {
                throw ExUtils.RuntimeException("Index reference count underflow in {0}.", this);
            }
            --indexRefN;
        }

        internal bool ReferencedByIndex() => indexRefN != 0;

        internal bool Overwritable()
        {
            if (allRefN != 0)
            {
                throw ExUtils.RuntimeException("Version should only be tested for overwrite when "
                    + "referenced at least once in {0}.", this);
            }
            return (allRefN == 1) && HasNoParents();
        }

        internal void AddStage(in Stage<ContentType, SharedContentType> stage) => inStages.Add(stage);

        internal void RemoveStage(in Stage<ContentType, SharedContentType> stage) => inStages.Remove(stage);

        internal void IncLinksToEmbeddedVersion(in Version<ContentType, SharedContentType> linkVersion)
        {
            if (!embeddingLinksN.ContainsKey(linkVersion))
            {
                embeddingLinksN.Add(linkVersion, 1);
                return;
            }
            embeddingLinksN[linkVersion]++;
        }

        internal void DecLinksToEmbeddedVersion(in Version<ContentType, SharedContentType> linkVersion)
        {
            if (!embeddingLinksN.ContainsKey(linkVersion))
            {
                throw ExUtils.RuntimeException("Linked embedded version {0} not present in {1}.", 
                    linkVersion, this);
            }
            if (--embeddingLinksN[linkVersion] == 0) _ = embeddingLinksN.Remove(linkVersion);
            
        }

        internal void IncParentN() => ++parentN;
        internal void DecParentN()
        {
            if (parentN == 0)
            {
                throw ExUtils.RuntimeException("Parent count underflow in {0}.", this);
            }
            --parentN;
        }

        internal bool HasNoParents() => parentN == 0;

        internal bool IsRoot() => BaseVersion == null;

        public override string ToString()
        {
            return String.Format("Version<{0}>", GetHashCode());
        }
    }
}
