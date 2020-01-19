using System;
using System.Collections.Generic;

namespace TheLookingGlass.DeepClone
{
    internal class IndexedAttributes : List<Attribute>
    {
        internal readonly Dictionary<Type, Attribute> Index = new Dictionary<Type, Attribute>();

        internal IndexedAttributes(List<Attribute> attrs)
        {
            if (attrs == null) return;
            foreach (var attr in attrs) Add(attr);
        }

        internal new void Add(Attribute attr)
        {
            Index.SafeTryAdd(attr.GetType(), attr, true);
            base.Add(attr);
        }
    }
}
