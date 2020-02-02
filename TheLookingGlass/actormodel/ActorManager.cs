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

        internal ActorCreationId GetCreationId() => Interlocked.Increment(ref _creationIdCounter);

        public struct ActorCreationId
        {
            private readonly long _x;

            private ActorCreationId(long x) => _x = x;

            public static implicit operator ActorCreationId(long value) => new ActorCreationId(value);

            public static implicit operator long(ActorCreationId record) => record._x;
        }
    }
}
