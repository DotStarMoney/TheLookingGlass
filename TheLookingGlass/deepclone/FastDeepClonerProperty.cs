using System;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;

namespace TheLookingGlass.DeepClone
{
    public class FastDeepClonerProperty
    {
        public Func<object, object> GetMethod { get; set; }

        public Action<object, object> SetMethod { get; set; }

        public bool CanRead { get; }

        public bool CanWrite { get; }

        public bool ReadAble { get; }

        public bool FastDeepClonerIgnore => ContainAttribute<FastDeepClonerIgnore>();

        public string Name { get; }

        public string FullName { get; }

        public bool IsInternalType { get;  }

        public Type PropertyType { get; set; }

        public bool? IsVirtual { get; }

        public IndexedAttributes IndexedAttributes { get; set; }

        public MethodInfo PropertyGetValue { get; }

        public MethodInfo PropertySetValue { get; }
        internal FastDeepClonerProperty(FieldInfo field)
        {
            CanRead = !(field.IsInitOnly || field.FieldType == typeof(IntPtr) || field.IsLiteral);
            CanWrite = CanRead;
            ReadAble = CanRead;
            GetMethod = field.GetValue;
            SetMethod = field.SetValue;
            Name = field.Name;
            FullName = field.FieldType.FullName;
            PropertyType = field.FieldType;
            IndexedAttributes = new IndexedAttributes(field.GetCustomAttributes().ToList());
            IsInternalType = field.FieldType.IsInternalType();
        }

        internal FastDeepClonerProperty(PropertyInfo property)
        {
            CanRead = !(!property.CanWrite || !property.CanRead || property.PropertyType == typeof(IntPtr) || property.GetIndexParameters().Length > 0);
            CanWrite = property.CanWrite;
            ReadAble = property.CanRead;
            GetMethod = property.GetValue;
            SetMethod = property.SetValue;
            Name = property.Name;
            FullName = property.PropertyType.FullName;
            IsInternalType = property.PropertyType.IsInternalType();
            IsVirtual = property.GetMethod.IsVirtual;
            PropertyGetValue = property.GetMethod;
            PropertySetValue = property.SetMethod;
            PropertyType = property.PropertyType;
            IndexedAttributes = new IndexedAttributes(property.GetCustomAttributes().ToList());

        }

        public bool ContainAttribute<T>() where T : Attribute
        {
            return IndexedAttributes?.Index.ContainsKey(typeof(T)) ?? false;
        }

        public void SetValue(object o, object value)
        {
            SetMethod(o, value);
        }

        public object GetValue(object o)
        {
            return GetMethod(o);
        }
    }
}
