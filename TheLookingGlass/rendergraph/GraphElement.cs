using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheLookingGlass.RenderGraph
{
    public abstract class GraphElement
    {
        protected readonly Graph owner;

        public string Name { get; }

        protected GraphElement(in Graph owner, in string name = null)
        {
            this.owner = owner;
            this.Name = owner.AddAndGetUniqueName(name == null ? DefaultName() : name, this);
        }

        protected abstract string DefaultName();
    }
}
