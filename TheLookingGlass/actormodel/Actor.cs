using TheLookingGlass.DeepClone;

namespace TheLookingGlass.ActorModel
{
    public abstract class Actor<TState> : ActorBase
    {
        private readonly TState _state;

        protected Actor(ActorManager manager) : base(manager)
        {


        }

        protected ref readonly TState State => ref _state;

        protected ref readonly TState MutableState
        {
            get
            {
                Updates.Mutated = true;
                return ref _state;
            }
        }
    }
}
