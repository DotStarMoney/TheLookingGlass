using System;
using System.Collections.Generic;
using System.Linq;

namespace TheLookingGlass.DeepClone
{
    /// <summary>
    /// DeepCloner
    /// </summary>
    public static class DeepCloner
    {
        /// <summary>

        public static T DeepClone<T>(this T objectToBeCloned) where T : class
        {
            return (T)new ReferenceClone().Clone(objectToBeCloned);
        }
    }
}
