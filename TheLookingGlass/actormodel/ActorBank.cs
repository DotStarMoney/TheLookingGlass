using System.Collections.Generic;
using System.Diagnostics.Contracts;
using TheLookingGlass.DeepClone;

namespace TheLookingGlass.ActorModel
{
    public class ActorBank
    {
        public enum Group
        {
            Active = 0,
            Sleeping = 1
        }

        private const int GroupN = 2;

        private readonly IList<ActorManager.ActorCreationId> _deletedCreationIds;

        private readonly Dictionary<ActorManager.ActorCreationId, ActorBankRecord>[] _groups =
            new Dictionary<ActorManager.ActorCreationId, ActorBankRecord>[GroupN];

        public ActorBank()
        {
            _deletedCreationIds = new List<ActorManager.ActorCreationId>();
        }

        private ActorBank(in IList<ActorManager.ActorCreationId> deletedCreationIds)
        {
            _deletedCreationIds = new List<ActorManager.ActorCreationId>(deletedCreationIds);
        }

        public void Add(ActorBase actor, in Group initialGroup = Group.Active)
        {
            Contract.Assert(actor.IsDetached());

            var record = new ActorBankRecord(this, actor, initialGroup);
            _groups[(int) initialGroup].Add(actor.CreationId, record);

            record.CreatedSinceLastClone = true;
            record.Stable = false;

            actor.ParentRecord = record;
        }

        public void Remove(ActorBase actor)
        {
            var record = actor.ParentRecord;
            Contract.Assert(record.Parent == this);

            var group = _groups[(int) record.Group];
            if (!record.CreatedSinceLastClone) _deletedCreationIds.Add(actor.CreationId);
            group.Remove(actor.CreationId);

            actor.Detach();
        }

        public void ChangeActorGroup(ActorBase actor, in Group newGroup)
        {
            var record = actor.ParentRecord;
            Contract.Assert(record.Parent == this);
            if (record.Group == newGroup) return;

            record.Stable = false;
            record.Group = newGroup;

            _groups[(int) record.Group].Remove(actor.CreationId);
            _groups[(int) newGroup].Add(actor.CreationId, record);
        }

        public void MergeIn(in ActorBank bank)
        {
            foreach (var deletedId in bank._deletedCreationIds)
            {
                for (var g = 0; g < GroupN; ++g) _groups[g].Remove(deletedId);
            }

            for (var g = 0; g < GroupN; ++g)
            {
                foreach (var record in bank._groups[g].Values) AddOrUpdateRecord(record);
            }
        }

        private void AddOrUpdateRecord(in ActorBankRecord record)
        {
            var existingRecord = GetMatchingRecord(record);
            if (existingRecord == null)
            {
                CloneRecordIntoBank(this, record);
            }
            else
            {
                existingRecord.Actor = record.Actor.DeepClone();
            }
        }

        private ActorBankRecord GetMatchingRecord(in ActorBankRecord record)
        {
            ActorBankRecord existingRecord;
            _groups[(int) record.Group].TryGetValue(record.Actor.CreationId, out existingRecord);
            if (existingRecord != null) return existingRecord;

            for (var g = 0; g < GroupN; ++g)
            {
                if ((Group) g == record.Group) continue;
                _groups[g].TryGetValue(record.Actor.CreationId, out existingRecord);
                if (existingRecord != null) return existingRecord;
            }

            return null;
        }

        public ActorBank Clone(in bool deltaClone = false)
        {
            var newBank = deltaClone ? new ActorBank(_deletedCreationIds) : new ActorBank();
            _deletedCreationIds.Clear();

            for (var g = 0; g < GroupN; ++g)
            {
                foreach (var record in _groups[g].Values)
                {
                    if (deltaClone && record.Stable) continue;
                    CloneRecordIntoBank(newBank, record);
                }
            }

            return newBank;
        }

        private static void CloneRecordIntoBank(ActorBank parent, in ActorBankRecord record)
        {
            var newActor = record.Actor.DeepClone();
            var newRecord = new ActorBankRecord(parent, newActor, record.Group);
            newActor.ParentRecord = newRecord;

            newRecord.CreatedSinceLastClone = false;
            newRecord.Stable = true;

            parent._groups[(int) record.Group].Add(newActor.CreationId, record);
        }

        internal class ActorBankRecord
        {
            internal ActorBase Actor;

            internal bool CreatedSinceLastClone;

            internal Group Group;

            internal ActorBank Parent;

            internal bool Stable;

            internal ActorBankRecord(in ActorBank parent, in ActorBase actor, in Group group)
            {
                Actor = actor;
                Parent = parent;
                Group = group;
            }
        }
    }
}
