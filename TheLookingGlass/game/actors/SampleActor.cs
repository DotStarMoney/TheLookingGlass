using TheLookingGlass.ActorModel;
using TheLookingGlass.DeepClone;

namespace TheLookingGlass.Game.Actors
{
    class SampleActor : Actor
    {
        private string _hidden;
        private int _savedInt;

        public SampleActor(int anInt, string test)
        {
            _savedInt = anInt;
            _hidden = test;
        }
    }
}
