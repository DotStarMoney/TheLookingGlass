using System.Diagnostics;
using TheLookingGlass.DeepClone;
using TheLookingGlass.Util;

namespace TheLookingGlass.ActorModel
{
    public abstract class ActorBase
    {

        internal readonly ActorManager.ActorCreationId CreationId;

        protected ActorBase(ActorManager manager)
        {
            CreationId = manager.GetCreationId();
        }

        [Uncloneable] internal ActorBank.ActorBankRecord ParentRecord { get; set; }

        protected void MarkMutated()
        {
            if (IsDetached()) ExUtils.RuntimeException("Actor mutation attempted when detached from bank.");
            ParentRecord.Stable = false;
        }

        internal bool IsDetached() => ParentRecord == null;

        internal void Detach() => ParentRecord = null;
    }
}
