using System;

namespace TheLookingGlass.Util
{
    public static class ExUtils
    {
        public static InvalidOperationException RuntimeException(
            in string exceptionString, params object[] args)
        {
            return new InvalidOperationException(string.Format(exceptionString, args));
        }
    }
}
