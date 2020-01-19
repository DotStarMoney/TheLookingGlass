using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
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
            var key = type.FullName + string.Join("", parameters?.Select(x => x.GetType()));
            if (CachedConstructorInfo.ContainsKey(key))
                return CachedConstructorInfo.SafeGet(key);

            IEnumerable<ConstructorInfo> constructors = type.GetConstructors();

            ConstructorInfo constructor = null;
            foreach (var cr in constructors)
            {
                var index = 0;
                var args = cr.GetParameters();
                if (args.Length == parameters.Length)
                {
                    var apply = true;
                    foreach (var pr in args)
                    {
                        var prType = pr.ParameterType;
                        var paramType = parameters[index].GetType();

                        if (prType != paramType && prType != typeof(object))
                        {
                            try
                            {
                                if ((prType.IsInternalType() && paramType.IsInternalType()))
                                {
                                    Convert.ChangeType(parameters[index], prType);
                                }
                                else
                                {
                                    if (prType.GetTypeInfo().IsInterface && paramType.GetTypeInfo().IsAssignableFrom(prType.GetTypeInfo()))
                                        continue;
                                    else
                                    {
                                        apply = false;
                                        break;
                                    }
                                }
                            }
                            catch
                            {
                                apply = false;
                                break;
                            }
                        }
                        index++;

                    }
                    if (apply)
                        constructor = cr;
                }
            }

            return CachedConstructorInfo.SafeGetOrAdd(key, constructor);
        }

        internal static object Creator(this Type type, bool validateArgs = true, params object[] parameters)
        {
            try
            {
                var key = type.FullName + string.Join("", parameters?.Select(x => x.GetType().FullName));
                var constructor = type.GetConstructorInfo(parameters ?? new object[0]);
                if (constructor == null && parameters?.Length > 0)
                    constructor = type.GetConstructorInfo(new object[0]);
                if (constructor != null)
                {
                    var constParam = constructor.GetParameters();
                    if (validateArgs && (parameters?.Any() ?? false))
                    {
                        for (var i = 0; i < parameters.Length; i++)
                        {
                            if (constParam.Length <= i)
                                continue;
                            if (constParam[i].ParameterType != parameters[i].GetType())
                            {
                                try
                                {
                                    parameters[i] = Convert.ChangeType(parameters[i], constParam[i].ParameterType);
                                }
                                catch
                                {
                                    // Ignore
                                }
                            }
                        }
                    }

                    if (!constParam.Any())
                    {
                        if (CachedDynamicMethods.ContainsKey(key))
                            return CachedDynamicMethods[key]();
                    }
                    else if (CachedDynamicMethodsWithParameters.ContainsKey(key))
                        return CachedDynamicMethodsWithParameters[key](parameters);

                    lock (CachedDynamicMethods)
                    {
                        var dynamicMethod = new System.Reflection.Emit.DynamicMethod("CreateInstance", type, (constParam.Any() ? new Type[] { typeof(object[]) } : Type.EmptyTypes), true);
                        System.Reflection.Emit.ILGenerator ilGenerator = dynamicMethod.GetILGenerator();


                        if (constructor.GetParameters().Any())
                        {

                            for (int i = 0; i < constParam.Length; i++)
                            {
                                Type paramType = constParam[i].ParameterType;
                                ilGenerator.Emit(System.Reflection.Emit.OpCodes.Ldarg_0); // Push array (method argument)
                                ilGenerator.Emit(System.Reflection.Emit.OpCodes.Ldc_I4, i); // Push i
                                ilGenerator.Emit(System.Reflection.Emit.OpCodes.Ldelem_Ref); // Pop array and i and push array[i]
                                if (paramType.IsValueType)
                                {
                                    ilGenerator.Emit(System.Reflection.Emit.OpCodes.Unbox_Any, paramType); // Cast to Type t
                                }
                                else
                                {
                                    ilGenerator.Emit(System.Reflection.Emit.OpCodes.Castclass, paramType); //Cast to Type t
                                }
                            }
                        }


                        //ilGenerator.Emit(System.Reflection.Emit.OpCodes.Nop);
                        ilGenerator.Emit(System.Reflection.Emit.OpCodes.Newobj, constructor);
                        //ilGenerator.Emit(System.Reflection.Emit.OpCodes.Stloc_1); // nothing
                        ilGenerator.Emit(System.Reflection.Emit.OpCodes.Ret);

                        if (!constParam.Any())
                            return CachedDynamicMethods.SafeGetOrAdd(key, (ObjectActivator)dynamicMethod.CreateDelegate(typeof(ObjectActivator)))();
                        
                        return CachedDynamicMethodsWithParameters.SafeGetOrAdd(key, (ObjectActivatorWithParameters)dynamicMethod.CreateDelegate(typeof(ObjectActivatorWithParameters)))(parameters);
                    }

                }
                else
                {
                    return FormatterServices.GetUninitializedObject(type);
                }
            }
            catch (Exception e)
            {
                throw e;
            }
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

            if ((type.GetTypeInfo().BaseType != null)
                && (type.GetTypeInfo().BaseType.Name != "Object"))
            {
                foreach (var runtimeProperty in type.GetTypeInfo().BaseType.GetRuntimeProperties())
                {
                    properties.SafeTryAdd(runtimeProperty.Name, new ObjectVariable(runtimeProperty));
                }
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
