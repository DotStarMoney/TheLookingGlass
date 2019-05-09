using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheLookingGlass.RenderGraph
{
    public interface IEdge
    {
        void Connect(IOutNode from, IInNode to);
    }
}
