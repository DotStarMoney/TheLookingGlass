using System.Collections.Generic;

namespace TheLookingGlass.DeepClone
{
    public static class DictionaryExtensions
    {
        public static void SafeTryAdd<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key,
            TValue value, bool overwrite = false)
        {
            if (dict.ContainsKey(key))
            {
                if (overwrite) dict[key] = value;
            }
            else
            {
                dict.Add(key, value);
            }
        }

        // Explicitly storing nulls in the dictionary is incompatible with this method.
        public static TValue SafeGetOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key,
            TValue value, bool overwrite = false)
        {
            dict.TryGetValue(key, out var existingValue);
            if (existingValue != null)
            {
                if (overwrite)
                {
                    dict[key] = value;
                }
                else
                {
                    return existingValue;
                }
            }
            else
            {
                dict.Add(key, value);
            }

            return value;
        }

        public static TValue SafeGet<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key)
        {
            dict.TryGetValue(key, out var existingValue);
            return existingValue;
        }
    }
}
