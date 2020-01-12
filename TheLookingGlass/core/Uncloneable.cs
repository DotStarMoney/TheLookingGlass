using System;

namespace TheLookingGlass.Core
{
    [AttributeUsage(System.AttributeTargets.Field, AllowMultiple = true)]
    public class Uncloneable : Attribute { }
}
