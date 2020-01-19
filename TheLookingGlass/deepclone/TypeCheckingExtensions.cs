using System;
using System.Collections.Generic;
using System.Reflection;

namespace TheLookingGlass.DeepClone
{
    internal static class TypeCheckingExtensions
    {
        private static readonly Dictionary<Type, int> InternalTypes = new Dictionary<Type, int>
        {
            {typeof(int), 0},
            {typeof(double), 0},
            {typeof(float), 0},
            {typeof(bool), 0},
            {typeof(decimal), 0},
            {typeof(long), 0},
            {typeof(DateTime), 0},
            {typeof(ushort), 0},
            {typeof(short), 0},
            {typeof(sbyte), 0},
            {typeof(byte), 0},
            {typeof(ulong), 0},
            {typeof(uint), 0},
            {typeof(char), 0},
            {typeof(TimeSpan), 0},
            {typeof(decimal?), 0},
            {typeof(int?), 0},
            {typeof(double?), 0},
            {typeof(float?), 0},
            {typeof(bool?), 0},
            {typeof(long?), 0},
            {typeof(DateTime?), 0},
            {typeof(ushort?), 0},
            {typeof(short?), 0},
            {typeof(sbyte?), 0},
            {typeof(byte?), 0},
            {typeof(ulong?), 0},
            {typeof(uint?), 0},
            {typeof(char?), 0},
            {typeof(TimeSpan?), 0},
            {typeof(string), 0},
            {typeof(Enum), 0},
            {typeof(byte[]), 0}
        };

        internal static bool IsInternalObject(this object o) => o is Enum;

        internal static bool IsAnonymousType(this Type type)
        {
            return type.Name.Contains("AnonymousType") 
                   && (type.Name.StartsWith("<>") || type.Name.StartsWith("VB$"));
        }

        internal static bool IsInternalType(this Type underlyingSystemType)
        {
            return (InternalTypes.ContainsKey(underlyingSystemType)
                    || !underlyingSystemType.GetTypeInfo().IsClass)
                   && !underlyingSystemType.GetTypeInfo().IsInterface;
        }
    }
}
