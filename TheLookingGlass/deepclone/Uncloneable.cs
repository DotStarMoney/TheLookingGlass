using System;

namespace TheLookingGlass.DeepClone
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
    public sealed class Uncloneable : Attribute { }
}
