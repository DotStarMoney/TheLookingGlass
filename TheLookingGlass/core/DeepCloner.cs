using System;
using System.Collections;
using System.Collections.Generic;

namespace TheLookingGlass.Core
{
    public class DeepCloner
    {
        public static object Clone(in object o, in Type t)
        {
            if (o is string)
            {
                return String.Copy((string)o);
            }
            else if (t.IsPrimitive)
            {
                switch (Type.GetTypeCode(t))
                {
                    case TypeCode.Boolean:
                        return (bool)o;
                    case TypeCode.Byte:
                        return (byte)o;
                    case TypeCode.SByte:
                        return (sbyte)o;
                    case TypeCode.Char:
                        return (char)o;
                    case TypeCode.Double:
                        return (double)o;
                    case TypeCode.Single:
                        return (float)o;
                    case TypeCode.Int32:
                        return (int)o;
                    case TypeCode.UInt32:
                        return (uint)o;
                    case TypeCode.Int64:
                        return (long)o;
                    case TypeCode.UInt64:
                        return (ulong)o;
                    case TypeCode.Int16:
                        return (short)o;
                    case TypeCode.UInt16:
                        return (ushort)o;
                }
            }
            else if (t.IsArray)
            {
                Array array = (Array)o;
                

            }
            else if (t.IsGenericType)
            {
                if (o is IList)
                {

                }
                else if (o is IDictionary)
                {

                }
            }
            
        }
    }
}
