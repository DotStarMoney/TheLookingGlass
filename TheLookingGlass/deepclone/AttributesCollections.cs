using System;
using System.Collections.Generic;

namespace TheLookingGlass.DeepClone
{
    public class AttributesCollections : List<Attribute>
    {
        internal readonly Dictionary<Type, Attribute> ContainedAttributestypes = new Dictionary<Type, Attribute>();

        public AttributesCollections(List<Attribute> attrs)
        {
            if (attrs == null)
                return;
            foreach (var attr in attrs)
                Add(attr);
        }

        public new void Add(Attribute attr)
        {
            ContainedAttributestypes.SafeTryAdd(attr.GetType(), attr, true);
            base.Add(attr);

        }
    }
}
