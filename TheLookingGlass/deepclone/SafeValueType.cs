using System.Collections.Generic;

namespace TheLookingGlass.DeepClone
{
    /// <summary>
    /// CustomValueType Dictionary
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="P"></typeparam>
    public class SafeValueType<T, P> : Dictionary<T, P>
    {
        public bool TryAdd(T key, P item, bool overwrite = false)
        {
            if (base.ContainsKey(key) && !overwrite)
                return true;

            if (overwrite && this.ContainsKey(key))
                this.Remove(key);
            base.Add(key, item);


            return true;
        }

        public P GetOrAdd(T key, P item, bool overwrite = false)
        {
            if (base.ContainsKey(key) && !overwrite)
                return base[key];

            if (overwrite && this.ContainsKey(key))
                this.Remove(key);
            base.Add(key, item);
            return base[key];
        }

        public P Get(T key)
        {
            if (this.ContainsKey(key))
                return this[key];
            object o = null;
            return (P)o;
        }
    }
}
