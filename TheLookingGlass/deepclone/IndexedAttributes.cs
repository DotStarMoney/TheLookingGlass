using System;
using System.Collections.Generic;

namespace TheLookingGlass.DeepClone
{
    public class IndexedAttributes : List<Attribute>
    {
        internal readonly Dictionary<Type, Attribute> Index = new Dictionary<Type, Attribute>();

        public IndexedAttributes(List<Attribute> attrs)
        {
            if (attrs == null) return;
            foreach (var attr in attrs) Add(attr);
        }

        public new void Add(Attribute attr)
        {
            Index.SafeTryAdd(attr.GetType(), attr, true);
            base.Add(attr);
        }
    }
}
