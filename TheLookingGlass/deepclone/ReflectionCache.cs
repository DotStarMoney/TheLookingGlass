using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;

namespace TheLookingGlass.DeepClone
{
    internal static class ReflectionCache
    {
        internal delegate object ObjectActivator();
        internal delegate object ObjectActivatorWithParameters(params object[] args);

        private static readonly Dictionary<Type, Dictionary<string, ObjectVariable>> CachedFields = 
            new Dictionary<Type, Dictionary<string, ObjectVariable>>();

        private static readonly Dictionary<Type, Dictionary<string, ObjectVariable>> CachedProperties = 
            new Dictionary<Type, Dictionary<string, ObjectVariable>>();

        private static readonly Dictionary<Type, Type> CachedIListTypes = 
            new Dictionary<Type, Type>();

        private static readonly Dictionary<string, ConstructorInfo> CachedConstructorInfo = 
            new Dictionary<string, ConstructorInfo>();

        private static readonly Dictionary<string, ObjectActivator> CachedDynamicMethods = 
            new Dictionary<string, ObjectActivator>();

        private static readonly Dictionary<string, ObjectActivatorWithParameters> 
            CachedDynamicMethodsWithParameters = new Dictionary<string, ObjectActivatorWithParameters>();

        internal static ConstructorInfo GetConstructorInfo(this Type type, params object[] parameters)
        {
            var cacheKey = parameters == null
                ? type.FullName
                : string.Concat(type.FullName, parameters.Select(x => x.GetType().FullName));
            if (CachedConstructorInfo.ContainsKey(cacheKey))
            {
                return CachedConstructorInfo.SafeGet(cacheKey);
            }

            IEnumerable<ConstructorInfo> constructors = type.GetConstructors();

            ConstructorInfo constructor = null;
            foreach (var cr in constructors)
            {
                var args = cr.GetParameters();

                if ((parameters == null) || (args.Length != parameters.Length)) continue;

                var apply = true;
                var index = 0;
                foreach (var pr in args)
                {
                    var prType = pr.ParameterType;
                    var paramType = parameters[index].GetType();
                    if (!CheckConstructorParam(prType, paramType, parameters[index]))
                    {
                        apply = false;
                        break;
                    }
                
                    index++;
                }
                if (apply) constructor = cr;
            }

            return CachedConstructorInfo.SafeGetOrAdd(cacheKey, constructor);
        }

        private static bool CheckConstructorParam(in Type cTorParamType, in Type paramType, object paramValue)
        {
            if ((cTorParamType == paramType) || (cTorParamType == typeof(object))) return true;

            try
            {
                if (cTorParamType.IsInternalType() && paramType.IsInternalType())
                {
                    Convert.ChangeType(paramValue, cTorParamType);
                }
                else
                {
                    return cTorParamType.GetTypeInfo().IsInterface
                           && paramType.GetTypeInfo().IsAssignableFrom(cTorParamType.GetTypeInfo());
                }
            }
            catch
            {
                return false;
            }
            return true;
        }

        internal static object Creator(
            this Type type, in bool validateArgs = true, params object[] parameters)
        {
            var cacheKey = parameters == null
                ? type.FullName
                : string.Concat(type.FullName, parameters.Select(x => x.GetType().FullName));

            var constructor = type.GetConstructorInfo(parameters ?? new object[0]);
            if ((constructor == null) && (parameters?.Length > 0))
            {
                constructor = type.GetConstructorInfo(new object[0]);
            }

            if (constructor == null)
            {
                return FormatterServices.GetUninitializedObject(type);
            }

            var constParam = constructor.GetParameters();
            if (validateArgs && (parameters != null) && parameters.Any())
            {
                for (var i = 0; i < parameters.Length; i++)
                {
                    if ((constParam.Length <= i) || (constParam[i].ParameterType == parameters[i].GetType()))
                    {
                        continue;
                    }

                    try
                    {
                        parameters[i] = Convert.ChangeType(parameters[i], constParam[i].ParameterType);
                    }
                    catch
                    {
                        // ignored
                    }
                }
            }

            if (!constParam.Any())
            {
                if (CachedDynamicMethods.ContainsKey(cacheKey))
                {
                    return CachedDynamicMethods[cacheKey]();
                }
            }
            else if (CachedDynamicMethodsWithParameters.ContainsKey(cacheKey))
            {
                return CachedDynamicMethodsWithParameters[cacheKey](parameters);
            }


            var dynamicMethod = new DynamicMethod("CreateInstance", type,
                (constParam.Any() ? new[] {typeof(object[])} : Type.EmptyTypes), true);

            var ilGenerator = dynamicMethod.GetILGenerator();

            if (constructor.GetParameters().Any())
            {
                for (var i = 0; i < constParam.Length; i++)
                {
                    var paramType = constParam[i].ParameterType;
                    ilGenerator.Emit(OpCodes.Ldarg_0);    // Push array (method argument)
                    ilGenerator.Emit(OpCodes.Ldc_I4, i);  // Push i
                    ilGenerator.Emit(OpCodes.Ldelem_Ref); // Pop array and i and push array[i]
                    ilGenerator.Emit(
                        paramType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass,
                        paramType);
                }
            }

            ilGenerator.Emit(OpCodes.Newobj, constructor);
            ilGenerator.Emit(OpCodes.Ret);

            return !constParam.Any()
                ? CachedDynamicMethods.SafeGetOrAdd(cacheKey,
                    (ObjectActivator) dynamicMethod.CreateDelegate(typeof(ObjectActivator)))()
                : CachedDynamicMethodsWithParameters.SafeGetOrAdd(cacheKey,
                    (ObjectActivatorWithParameters) dynamicMethod.CreateDelegate(
                        typeof(ObjectActivatorWithParameters)))(parameters);
        }

        internal static Dictionary<string, ObjectVariable> GetCachedProperties(this Type type)
        {
            CachedProperties.TryGetValue(type, out var cachedProperties);
            if (cachedProperties != null) return cachedProperties;

            var properties = type.GetPropertiesFromType();
            CachedProperties.Add(type, properties);
            return properties;
        }

        internal static Dictionary<string, ObjectVariable> GetPropertiesFromType(this Type type)
        {
            var properties = new Dictionary<string, ObjectVariable>();
            foreach (var runtimeProperty in type.GetRuntimeProperties())
            {
                properties.SafeTryAdd(runtimeProperty.Name, new ObjectVariable(runtimeProperty));
            }

            if ((type.GetTypeInfo().BaseType == null) || (type.GetTypeInfo().BaseType.Name == "Object"))
            {
                return properties;
            }

            foreach (var runtimeProperty in type.GetTypeInfo().BaseType.GetRuntimeProperties())
            {
                properties.SafeTryAdd(runtimeProperty.Name, new ObjectVariable(runtimeProperty));
            }
            
            return properties;
        }

        internal static Dictionary<string, ObjectVariable> GetCachedFieldsExcludingProperties(
            this Type type, in Dictionary<string, ObjectVariable> typeProperties)
        {
            CachedFields.TryGetValue(type, out var cachedProperties);
            if (cachedProperties != null) return cachedProperties;

            var properties = new Dictionary<string, ObjectVariable>();
            foreach (var runtimeField in type.GetRuntimeFields())
            {
                if (typeProperties.ContainsKey(runtimeField.Name)) continue;
                properties.SafeTryAdd(runtimeField.Name, new ObjectVariable(runtimeField));
            }

            if ((type.GetTypeInfo().BaseType != null) 
                && (type.GetTypeInfo().BaseType.Name != "Object"))
            {
                foreach (var runtimeField in type.GetTypeInfo().BaseType.GetRuntimeFields())
                {
                    if (typeProperties.ContainsKey(runtimeField.Name)) continue;
                    properties.SafeTryAdd(runtimeField.Name, new ObjectVariable(runtimeField));
                }
            }

            CachedFields.Add(type, properties);
            return properties;
        }

        internal static Type GetIListType(this Type type)
        {
            CachedIListTypes.TryGetValue(type, out var cachedType);
            if (cachedType != null) return cachedType;

            Type iListType;
            if (type.IsArray)
            {
                iListType = type.GetElementType();
            }
            else
            {
                if (type.GenericTypeArguments.Any())
                {
                    if (type.FullName.Contains("ObservableCollection`1"))
                    {
                        iListType = typeof(ObservableCollection<>).MakeGenericType(
                            type.GenericTypeArguments.First());
                    }
                    else
                    {
                        iListType = typeof(List<>).MakeGenericType(type.GenericTypeArguments.First());
                    }
                }
                else if (type.FullName.Contains("List`1") || type.FullName.Contains("ObservableCollection`1"))
                {
                    if (type.FullName.Contains("ObservableCollection`1"))
                    {
                        iListType = typeof(ObservableCollection<>).MakeGenericType(
                            type.GetRuntimeProperty("Item").PropertyType);
                    }
                    else
                    {
                        iListType =
                            typeof(List<>).MakeGenericType(type.GetRuntimeProperty("Item").PropertyType);
                    }
                }
                else
                {
                    iListType = type;
                }
            }

            CachedIListTypes.Add(type, iListType);
            return iListType;
        }
    }
}
