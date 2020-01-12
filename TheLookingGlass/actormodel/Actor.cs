using System;
using System.Reflection;
using TheLookingGlass.Core;

namespace TheLookingGlass.ActorModel
{
    public abstract class Actor : IAdvanceable, IDeepCloneable
    {
        private static readonly BindingFlags CloneBindingConstants = 
            BindingFlags.Instance | BindingFlags.FlattenHierarchy 
                | BindingFlags.Public | BindingFlags.NonPublic;

        internal bool enabled = true; 

        void IAdvanceable.Advance(in double deltaT)
        {
            if (!enabled)
            {
                throw ExUtils.RuntimeException("Called Actor.Advance(...) while disabled.");
            }
            Advance(deltaT);
        }

        protected abstract void Advance(in double deltaT);

        public object DeepClone()
        {            
            Actor newActor = (Actor) Activator.CreateInstance(GetType());

            foreach (FieldInfo field in GetType().GetFields(CloneBindingConstants))
            {
                object value = field.GetValue(this);
                if (!Attribute.IsDefined(field, typeof(Uncloneable)))
                {
                    field.SetValue(
                        newActor,
                        value is IDeepCloneable
                            ? ((IDeepCloneable)value).DeepClone()
                            : DeepCloner.Clone(value, field.FieldType));

                }
            }

            return newActor;
        }
    }
}
 