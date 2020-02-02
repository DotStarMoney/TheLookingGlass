using TheLookingGlass.ActorModel;
using TheLookingGlass.DeepClone;

namespace TheLookingGlass.Game.Actors
{
    public class SampleActor : Actor<SampleActor.Vars>
    { 
        public class Vars
        {
            internal string _hidden;
            internal int _savedInt;
        }

        public SampleActor(in ActorManager actorManager) : base(actorManager)
        { 
            
        }

        private void DoStuff()
        {

            MutableState._hidden = "";

        }
    }
}
