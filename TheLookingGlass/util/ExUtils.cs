using System;

namespace TheLookingGlass
{
    public static class ExUtils
    {
        public static InvalidOperationException RuntimeException(in string exceptionString, 
            params object[] args)
        {
            return new InvalidOperationException(String.Format(exceptionString, args));
        }
    }
}
