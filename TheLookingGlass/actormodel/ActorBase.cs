using System.Diagnostics;
using TheLookingGlass.DeepClone;
using TheLookingGlass.Util;

namespace TheLookingGlass.ActorModel
{
    public abstract class ActorBase
    {
        internal struct UpdateSignals
        {
            public bool Mutated;

            public bool Exited;

            public ActorBank.Group Group;
        }

        internal UpdateSignals Updates;

        internal readonly ActorManager.ActorCreationId CreationId;

        protected ActorBase(ActorManager manager)
        {
            CreationId = manager.GetCreationId();
            Updates.Mutated = false;
            Updates.Exited = false;
            Updates.Group = ActorBank.Group.Unknown;
        }

        [Uncloneable] internal ActorBank.ActorBankRecord ParentRecord { get; set; }

        protected void ResetUpdates()
        {
            // Just the mutated flag needs resetting.
            Updates.Mutated = false;
        }

        protected void Exit() => Updates.Exited = true;

        protected void Sleep() => Updates.Group = ActorBank.Group.Sleeping;

        protected void Awake() => Updates.Group = ActorBank.Group.Active;

        internal bool IsDetached() => ParentRecord == null;

        internal void Detach()
        {
            ParentRecord = null;
            Updates.Group = ActorBank.Group.Unknown;
        }
    }
}
