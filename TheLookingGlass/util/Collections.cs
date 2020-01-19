using System.Collections.Generic;

namespace TheLookingGlass.Util
{
    public static class Collections
    {
        public static IEnumerable<T> Of<T>(params T[] list)
        {
            return new List<T>(list);
        }

        public static T GetOnlyElement<T>(in IEnumerable<T> enumerable)
        {
            var onlyElement = default(T);
            var passed = false;
            foreach (var element in enumerable)
            {
                if (passed) throw ExUtils.RuntimeException("Enumerable had more than one element.");
                onlyElement = element;
                passed = true;
            }

            if (!passed) throw ExUtils.RuntimeException("Enumerable had no elements.");
            return onlyElement;
        }
    }
}
