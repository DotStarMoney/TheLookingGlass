using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace TheLookingGlass.RenderGraph.Primitives
{
    public class Line2 : GraphElement, IOutNode
    {
        protected override string DefaultName() => "Primitives.Line2";

        public Vector2 Start { get; set; }

        public Vector2 End { get; set; }

        public Vector3 Color { get; set; }

        public Line2(
            in Graph owner, 
            in Vector2 a, 
            in Vector2 b, 
            in Vector3 color, 
            in string name = null) : base(owner, name)
        {
            this.Start = a;
            this.End = b;
            this.Color = color;
        }


    }
}
