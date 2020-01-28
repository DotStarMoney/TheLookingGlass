using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TheLookingGlass.ActorModel
{
    public class ActorManager
    {
        private long _creationIdCounter = 0;

        internal long GetCreationId() => Interlocked.Increment(ref _creationIdCounter);
    }
}
