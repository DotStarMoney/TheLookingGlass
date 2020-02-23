using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheLookingGlass.ActorModel
{
    interface IAdvanceable
    {
        void Advance(float deltaT);
    }
}
