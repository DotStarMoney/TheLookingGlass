using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheLookingGlass.RenderGraph
{
    public sealed class Graph
    {
        private Dictionary<string, ElementWithDuplicateNameCount> elements;

        internal string AddAndGetUniqueName(in string name, in GraphElement element)
        {
            ElementWithDuplicateNameCount existingElement;
            string newName;
            if (elements.TryGetValue(name, out existingElement))
            {
                newName = String.Format("{0}_{1}", name, existingElement.index);
                ++existingElement.index;
            }
            else
            {
                newName = name;
            }
            elements.Add(newName, new ElementWithDuplicateNameCount(element));
            return newName;
        }

        private sealed class ElementWithDuplicateNameCount
        {
            internal GraphElement Element { get; }

            internal int index;

            internal ElementWithDuplicateNameCount(in GraphElement element)
            {
                this.Element = element;
                this.index = 1;
            }
        }
    }
}
