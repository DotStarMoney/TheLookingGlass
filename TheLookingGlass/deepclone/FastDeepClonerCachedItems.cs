using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;


namespace TheLookingGlass.DeepClone
{
    internal static class FastDeepClonerCachedItems
    {
        internal delegate object ObjectActivator();
        internal delegate object ObjectActivatorWithParameters(params object[] args);
        private static readonly SafeValueType<Type, SafeValueType<string, IFastDeepClonerProperty>> CachedFields = new SafeValueType<Type, SafeValueType<string, IFastDeepClonerProperty>>();
        private static readonly SafeValueType<Type, SafeValueType<string, IFastDeepClonerProperty>> CachedPropertyInfo = new SafeValueType<Type, SafeValueType<string, IFastDeepClonerProperty>>();
        private static readonly SafeValueType<Type, Type> CachedTypes = new SafeValueType<Type, Type>();
        private static readonly SafeValueType<string, ConstructorInfo> ConstructorInfo = new SafeValueType<string, ConstructorInfo>();
        private static readonly SafeValueType<string, ObjectActivator> CachedDynamicMethod = new SafeValueType<string, ObjectActivator>();
        private static readonly SafeValueType<string, ObjectActivatorWithParameters> CachedDynamicMethodWithParameters = new SafeValueType<string, ObjectActivatorWithParameters>();

        internal static ConstructorInfo GetConstructorInfo(this Type type, params object[] parameters)
        {

            IEnumerable<ConstructorInfo> constructors;
            var key = type.FullName + string.Join("", parameters?.Select(x => x.GetType()));
            if (ConstructorInfo.ContainsKey(key))
                return ConstructorInfo.Get(key);

            constructors = type.GetConstructors();

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

            return ConstructorInfo.GetOrAdd(key, constructor);
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
                        if (CachedDynamicMethod.ContainsKey(key))
                            return CachedDynamicMethod[key]();
                    }
                    else if (CachedDynamicMethodWithParameters.ContainsKey(key))
                        return CachedDynamicMethodWithParameters[key](parameters);

                    lock (CachedDynamicMethod)
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
                            return CachedDynamicMethod.GetOrAdd(key, (ObjectActivator)dynamicMethod.CreateDelegate(typeof(ObjectActivator)))();
                        else
                        {
                            return CachedDynamicMethodWithParameters.GetOrAdd(key, (ObjectActivatorWithParameters)dynamicMethod.CreateDelegate(typeof(ObjectActivatorWithParameters)))(parameters);
                        }
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

        internal static Dictionary<string, IFastDeepClonerProperty> GetFastDeepClonerProperties(this Type primaryType)
        {
            if (!CachedPropertyInfo.ContainsKey(primaryType))
            {
                var properties = new SafeValueType<string, IFastDeepClonerProperty>();
                if (primaryType.GetTypeInfo().BaseType != null && primaryType.GetTypeInfo().BaseType.Name != "Object")
                {
                    primaryType.GetRuntimeProperties().Where(x => properties.TryAdd(x.Name, new FastDeepClonerProperty(x))).ToList();
                    primaryType.GetTypeInfo().BaseType.GetRuntimeProperties().Where(x => properties.TryAdd(x.Name, new FastDeepClonerProperty(x))).ToList();
                }
                else primaryType.GetRuntimeProperties().Where(x => properties.TryAdd(x.Name, new FastDeepClonerProperty(x))).ToList();
                CachedPropertyInfo.Add(primaryType, properties);
            }
            return CachedPropertyInfo[primaryType];
        }

        internal static Dictionary<string, IFastDeepClonerProperty> GetFastDeepClonerFields(this Type primaryType)
        {
            if (!CachedFields.ContainsKey(primaryType))
            {
                var properties = new SafeValueType<string, IFastDeepClonerProperty>();
                if (primaryType.GetTypeInfo().BaseType != null && primaryType.GetTypeInfo().BaseType.Name != "Object")
                {
                    primaryType.GetRuntimeFields().Where(x => properties.TryAdd(x.Name, new FastDeepClonerProperty(x))).ToList();
                    primaryType.GetTypeInfo().BaseType.GetRuntimeFields().Where(x => properties.TryAdd(x.Name, new FastDeepClonerProperty(x))).ToList();

                }
                else primaryType.GetRuntimeFields().Where(x => properties.TryAdd(x.Name, new FastDeepClonerProperty(x))).ToList();
                CachedFields.Add(primaryType, properties);
            }
            return CachedFields[primaryType];
        }

        internal static Type GetIListType(this Type type)
        {
            if (CachedTypes.ContainsKey(type))
                return CachedTypes[type];
            if (type.IsArray)
                CachedTypes.Add(type, type.GetElementType());
            else
            {
                if (type.GenericTypeArguments.Any())
                {
                    if (type.FullName.Contains("ObservableCollection`1"))
                        CachedTypes.Add(type, typeof(ObservableCollection<>).MakeGenericType(type.GenericTypeArguments.First()));
                    else
                        CachedTypes.Add(type, typeof(List<>).MakeGenericType(type.GenericTypeArguments.First()));
                }
                else if (type.FullName.Contains("List`1") || type.FullName.Contains("ObservableCollection`1"))
                {
                    if (type.FullName.Contains("ObservableCollection`1"))
                        CachedTypes.Add(type, typeof(ObservableCollection<>).MakeGenericType(type.GetRuntimeProperty("Item").PropertyType));
                    else
                        CachedTypes.Add(type, typeof(List<>).MakeGenericType(type.GetRuntimeProperty("Item").PropertyType));
                }
                else CachedTypes.Add(type, type);
            }
            return CachedTypes[type];
        }
    }
}
