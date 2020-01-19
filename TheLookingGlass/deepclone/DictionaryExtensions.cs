using System.Collections.Generic;

namespace TheLookingGlass.DeepClone
{
    internal static class DictionaryExtensions
    {
        internal static void SafeTryAdd<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key,
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

        internal static TValue SafeGetOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key,
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

        internal static TValue SafeGet<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key)
        {
            dict.TryGetValue(key, out var existingValue);
            return existingValue;
        }
    }
}
