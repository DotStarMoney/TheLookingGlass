using System;
using System.Reflection;
using System.Linq;

namespace TheLookingGlass.DeepClone
{
    internal class ObjectVariable
    {
        internal Func<object, object> GetMethod { get; set; }

        internal Action<object, object> SetMethod { get; set; }

        internal bool CanRead { get; }

        internal bool CanWrite { get; }

        internal bool ReadAble { get; }

        internal bool Uncloneable => ContainsAttribute<Uncloneable>();

        internal string Name { get; }

        internal string FullName { get; }

        internal bool IsInternalType { get;  }

        internal Type PropertyType { get; set; }

        internal bool? IsVirtual { get; }

        internal IndexedAttributes IndexedAttributes { get; set; }

        internal MethodInfo PropertyGetValue { get; }

        internal MethodInfo PropertySetValue { get; }
        internal ObjectVariable(FieldInfo field)
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

        internal ObjectVariable(PropertyInfo property)
        {
            CanRead = !(
                !property.CanWrite 
                || !property.CanRead 
                || (property.PropertyType == typeof(IntPtr) )
                || (property.GetIndexParameters().Length > 0));
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

        private bool ContainsAttribute<T>() where T : Attribute
        {
            return IndexedAttributes?.Index.ContainsKey(typeof(T)) ?? false;
        }

        internal void SetValue(object o, object value) => SetMethod(o, value);
        
        internal object GetValue(object o) => GetMethod(o);
    }
}
