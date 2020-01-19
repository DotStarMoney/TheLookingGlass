using System;
namespace TheLookingGlass.DeepClone
{
    /// <summary>
    /// Ignore Properties or Field that containe this attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
    public sealed class FastDeepClonerIgnore : Attribute
    {
    }
}
