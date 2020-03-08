using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using TheLookingGlass.DeepClone;

namespace TheLookingGlass.ActorModel
{
    public class ActorBank : IAdvanceable
    {
        public enum Group
        {
            // Actors perform their synchronous Advance method
            Active = 0,
            // Actors don't perform their synchronous Advance method (or don't have one)
            Sleeping = 1,
            // Unbound state.
            Unknown = -1
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

        public void Add(ActorBase actor, Group initialGroup = Group.Active)
        {
            Contract.Assert(actor.IsDetached());

            if (!(actor is IAdvanceable))
            {
                initialGroup = Group.Sleeping;
            }

            var record = new ActorBankRecord(this, actor, initialGroup);
            _groups[(int) initialGroup].Add(actor.CreationId, record);

            record.CreatedSinceLastClone = true;
            record.Stable = false;

            actor.ParentRecord = record;
            actor.Updates.Group = initialGroup;
        }

        public void Remove(ActorBase actor)
        {
            var record = actor.ParentRecord;
            Contract.Assert(record.Parent == this);

            var group = _groups[(int) record.Group];
            RemoveActorFromGroup(actor, group);

            actor.Detach();
        }

        public void ChangeActorGroup(ActorBase actor, in Group newGroup)
        {
            var record = actor.ParentRecord;
            // This should also catch any unbound actors (though doesn't tell us what is wrong).
            Contract.Assert(record.Parent == this);
            if (record.Group == newGroup) return;

            Contract.Assert((actor is IAdvanceable) || (newGroup == Group.Sleeping));
            
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
            _groups[(int) record.Group].TryGetValue(record.Actor.CreationId, out var existingRecord);
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

        public void Advance(float deltaT)
        {
            // TODO: Parallelize implementation

            // 1) Advance everyone who isn't asleep.
            var syncUpdateGroup = _groups[(int)Group.Active].Values;
            AdvanceActors(syncUpdateGroup, deltaT);

            // 2) Deal with anyone who requested a state update.
            ProcessActorUpdates(syncUpdateGroup);


            

        }

        // Assumes all actors are advanceable.
        private void AdvanceActors(IEnumerable<ActorBankRecord> actorRecords, float deltaT)
        {
            foreach (var record in actorRecords)
            {
                ((IAdvanceable)record.Actor).Advance(deltaT);
            }
        }

        // Book keeping to take care of actor update signals.
        private void ProcessActorUpdates(IEnumerable<ActorBankRecord> actorRecords)
        {
            foreach (var record in actorRecords)
            {
                var actor = record.Actor;
                var updates = record.Actor.Updates;

                if (updates.Exited)
                {
                    RemoveActorFromGroup(actor, _groups[(int) record.Group]);
                    actor.Detach();
                    continue;
                }

                // Note we ignore requests to switch groups for actors that are not advanceable.
                var stableGroup = (updates.Group == record.Group) || !(actor is IAdvanceable);
                if (!stableGroup)
                {
                    _groups[(int)record.Group].Remove(actor.CreationId);
                    _groups[(int)updates.Group].Add(actor.CreationId, record);
                    record.Group = updates.Group;
                }

                record.Stable = stableGroup && !updates.Mutated;
                actor.ResetUpdates();
                // We do this in case the actor requested a group switch and we ignored it.
                actor.Updates.Group = record.Group;
            }
        }

        private void RemoveActorFromGroup(
            ActorBase actor, Dictionary<ActorManager.ActorCreationId, ActorBankRecord> group)
        {
            if (!actor.ParentRecord.CreatedSinceLastClone) _deletedCreationIds.Add(actor.CreationId);
            group.Remove(actor.CreationId);
        }
    }
}
