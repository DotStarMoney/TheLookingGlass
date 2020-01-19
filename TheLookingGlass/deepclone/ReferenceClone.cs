using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace TheLookingGlass.DeepClone
{
    internal class ReferenceClone
    {
        private object ReferenceTypeClone(
            Dictionary<string, IFastDeepClonerProperty> properties, 
            Type primaryType, 
            object objectToBeCloned, 
            object appendToValue = null)
        {
            var resObject = appendToValue ?? primaryType.Creator();

            foreach (var property in properties.Values)
            {
                if (!property.CanRead || property.FastDeepClonerIgnore) continue;
                var value = property.GetValue(objectToBeCloned);
                if (value == null) continue;

                if (property.IsInternalType || value.GetType().IsInternalType())
                {
                    property.SetValue(resObject, value);
                }
                else
                {
                    property.SetValue(resObject, Clone(value));
                }
            }

            return resObject;
        }

        internal object Clone(object objectToBeCloned)
        {
            if (objectToBeCloned == null)
                return null;
            var primaryType = objectToBeCloned.GetType();
            if (primaryType.IsArray && primaryType.GetArrayRank() > 1)
                return ((Array) objectToBeCloned).Clone();

            if (objectToBeCloned.IsInternalObject())
                return objectToBeCloned;

            object resObject;
            if (primaryType.IsArray || objectToBeCloned is IList)
            {
                resObject = primaryType.IsArray
                    ? Array.CreateInstance(primaryType.GetIListType(), (objectToBeCloned as Array).Length)
                    : Activator.CreateInstance(primaryType.GetIListType());
                var i = 0;
                var ilist = resObject as IList;
                var array = resObject as Array;

                foreach (var item in objectToBeCloned as IList)
                {
                    object clonedIteam = null;
                    if (item != null) clonedIteam = item.GetType().IsInternalType() ? item : Clone(item);
                    if (!primaryType.IsArray)
                        ilist?.Add(clonedIteam);
                    else
                        array?.SetValue(clonedIteam, i);
                    i++;
                }

                foreach (var prop in primaryType.GetFastDeepClonerProperties().Where(x =>
                    typeof(List<string>).GetFastDeepClonerProperties().All(a => a.Key != x.Key)))
                {
                    var property = prop.Value;
                    if (!property.CanRead || property.FastDeepClonerIgnore)
                        continue;
                    var value = property.GetValue(objectToBeCloned);
                    if (value == null)
                        continue;
                    var clonedIteam = value.GetType().IsInternalType() ? value : Clone(value);
                    property.SetValue(resObject, clonedIteam);
                }
            }
            else if (objectToBeCloned is IDictionary)
            {
                resObject = Activator.CreateInstance(primaryType);
                var resDic = resObject as IDictionary;
                var dictionary = (IDictionary) objectToBeCloned;
                foreach (var key in dictionary.Keys)
                {
                    var item = dictionary[key];
                    object clonedIteam = null;
                    if (item != null) clonedIteam = item.GetType().IsInternalType() ? item : Clone(item);
                    resDic?.Add(key, clonedIteam);
                }
            }
            else if (primaryType.IsAnonymousType()) // dynamic types
            {
                var props = primaryType.GetFastDeepClonerProperties();
                resObject = new ExpandoObject();
                var d = resObject as IDictionary<string, object>;
                foreach (var prop in props.Values)
                {
                    var item = prop.GetValue(objectToBeCloned);
                    var value = item == null || prop.IsInternalType || (item?.IsInternalObject() ?? true)
                        ? item
                        : Clone(item);
                    if (!d.ContainsKey(prop.Name))
                        d.Add(prop.Name, value);
                }
            }
            else
            {
                resObject = ReferenceTypeClone(primaryType.GetFastDeepClonerProperties(), primaryType,
                    objectToBeCloned);
                resObject = ReferenceTypeClone(
                    primaryType.GetFastDeepClonerFields().Values.ToList()
                        .Where(x => !primaryType.GetFastDeepClonerProperties().ContainsKey(x.Name))
                        .ToDictionary(x => x.Name, x => x), primaryType, objectToBeCloned, resObject);
            }

            return resObject;
        }
    }
}
