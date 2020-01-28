using TheLookingGlass.DeepClone;

namespace TheLookingGlass.ActorModel
{
    public abstract class Actor
    {
        [Uncloneable] internal ActorBank ParentBank { get; set; }
        [Uncloneable] internal ActorBank.Group ParentBankGroup { get; set; }
        [Uncloneable] internal int ParentBankId { get; set; }

        internal readonly long CreationId;

        protected Actor(ActorManager manager)
        {
            CreationId = manager.GetCreationId();
        }

        internal bool IsDetached() => ParentBank == null;

        internal void Detach() => ParentBank = null;
    }
}
