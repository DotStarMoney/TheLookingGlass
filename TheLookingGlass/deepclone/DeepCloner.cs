using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace TheLookingGlass.DeepClone
{
    internal static class DeepCloner
    {
        private static readonly HashSet<string> StringListPropertyKeys = 
            new HashSet<string>(typeof(List<string>).GetCachedProperties().Keys);

        /// <summary>WARNING: This method is not even remotely thread-safe!</summary>
        public static T DeepClone<T>(this T objectToBeCloned) where T : class
        {
            return (T) DeepCloneImpl(objectToBeCloned);
        }

        internal static object DeepCloneImpl(in object objectToBeCloned)
        {
            if (objectToBeCloned == null) return null;

            var primaryType = objectToBeCloned.GetType();
            if (primaryType.IsArray && primaryType.GetArrayRank() > 1)
            {
                return ((Array) objectToBeCloned).Clone();
            }

            if (objectToBeCloned.IsInternalObject()) return objectToBeCloned;

            object clonedObject;
            if (primaryType.IsArray || (objectToBeCloned is IList))
            {
                clonedObject = primaryType.IsArray
                    ? Array.CreateInstance(primaryType.GetIListType(), (objectToBeCloned as Array).Length)
                    : Activator.CreateInstance(primaryType.GetIListType());

                var i = 0;
                var iList = clonedObject as IList;
                var array = clonedObject as Array;

                foreach (var item in (objectToBeCloned as IList))
                {
                    object clonedItem = null;
                    if (item != null)
                    {
                        clonedItem = item.GetType().IsInternalType() ? item : DeepCloneImpl(item);
                    }

                    if (!primaryType.IsArray)
                    {
                        iList?.Add(clonedItem);
                    }
                    else
                    {
                        array?.SetValue(clonedItem, i);
                    }
                    i++;
                }

                foreach (var prop in primaryType.GetCachedProperties().Where(
                    x => !StringListPropertyKeys.Contains(x.Key)))
                {
                    var property = prop.Value;
                    if (!property.CanRead || property.Uncloneable) continue;

                    var value = property.GetValue(objectToBeCloned);
                    if (value == null) continue;

                    var clonedItem = value.GetType().IsInternalType() ? value : DeepCloneImpl(value);
                    property.SetValue(clonedObject, clonedItem);
                }
            }
            else if (objectToBeCloned is IDictionary dictionary)
            {
                clonedObject = Activator.CreateInstance(primaryType);

                var clonedDict = clonedObject as IDictionary;
                foreach (var key in dictionary.Keys)
                {
                    var item = dictionary[key];

                    clonedDict.Add(
                        key, 
                        item != null
                            ? (item.GetType().IsInternalType() ? item : DeepCloneImpl(item)) 
                            : null);
                }
            }
            else if (primaryType.IsAnonymousType())
            {
                var props = primaryType.GetCachedProperties();

                clonedObject = new ExpandoObject();

                var dynamicObject = (IDictionary<string, object>) clonedObject;
                foreach (var prop in props.Values)
                {
                    var item = prop.GetValue(objectToBeCloned);
                    var value = (item == null || prop.IsInternalType || item.IsInternalObject())
                        ? item
                        : DeepCloneImpl(item);
                    if (!dynamicObject.ContainsKey(prop.Name)) dynamicObject.Add(prop.Name, value);
                }
            }
            else
            {
                var typeProperties = primaryType.GetCachedProperties();
                clonedObject = DeepCloneReference(typeProperties, primaryType, objectToBeCloned);

                var typeFields = primaryType.GetCachedFieldsExcludingProperties(typeProperties);
                clonedObject = DeepCloneReference(
                    typeFields, primaryType, objectToBeCloned, clonedObject);
            }

            return clonedObject;
        }

        private static object DeepCloneReference(
            in Dictionary<string, ObjectVariable> properties,
            in Type primaryType,
            in object objectToBeCloned,
            in object appendToValue = null)
        {
            var resObject = appendToValue ?? primaryType.Creator();

            foreach (var property in properties.Values)
            {
                if (!property.CanRead || property.Uncloneable) continue;

                var value = property.GetValue(objectToBeCloned);
                if (value == null) continue;

                if (property.IsInternalType || value.GetType().IsInternalType())
                {
                    property.SetValue(resObject, value);
                }
                else
                {
                    property.SetValue(resObject, DeepCloneImpl(value));
                }
            }

            return resObject;
        }
    }
}
