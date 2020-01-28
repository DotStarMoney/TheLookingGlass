using System.Collections.Generic;
using System.Diagnostics.Contracts;
using TheLookingGlass.Util;

namespace TheLookingGlass.ActorModel
{
    public class ActorBank
    {
        private const int GroupN = 3;

        public enum Group { Active = 0, Idling = 1, Sleeping = 2 }

        private struct ActorRecord
        { 
            internal readonly Actor Actor;
            internal readonly bool DeltaCloneable;

            internal ActorRecord(in Actor actor, in bool deltaCloneable)
            {
                Actor = actor;
                DeltaCloneable = deltaCloneable;
            }
        }

        private readonly ClaimCheck<ActorRecord>[] _groups = new ClaimCheck<ActorRecord>[GroupN];
        private readonly Dictionary<long, Actor>[] _creationIdToActor = new Dictionary<long, Actor>[GroupN];

        public void Add(Actor actor, in Group initialGroup = Group.Active)
        {
            Contract.Assert(actor.IsDetached());

            _creationIdToActor[(int)initialGroup][actor.CreationId] = actor;
            var claimId = _groups[(int)initialGroup].Add(new ActorRecord(actor, false));

            actor.ParentBank = this;
            actor.ParentBankGroup = initialGroup;
            actor.ParentBankId = claimId;
        }

        public void Remove(Actor actor)
        {
            Contract.Assert(actor.ParentBank == this);

            var group = (int) actor.ParentBankGroup;
            _groups[group].Remove(actor.ParentBankId);
            _creationIdToActor[group].Remove(actor.CreationId);

            actor.Detach();
        }

        public void ChangeActorGroup(Actor actor, in Group newGroup)
        {
            Contract.Assert(actor.ParentBank == this);
            if (actor.ParentBankGroup == newGroup) return;

            var group = (int)actor.ParentBankGroup;
            _groups[group].Remove(actor.ParentBankId);
            _creationIdToActor[group].Remove(actor.CreationId);

            _creationIdToActor[(int) newGroup][actor.CreationId] = actor;
            var claimId = _groups[(int) newGroup].Add(new ActorRecord(actor, false));

            actor.ParentBankGroup = newGroup;
            actor.ParentBankId = claimId;
        }

        public void MergeInActive(in ActorBank bank)
        {
            
        }

        public ActorBank Clone(in bool deltaClone = false)
        {

        }
    }
}
