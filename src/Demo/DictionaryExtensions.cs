using System;
using System.Collections.Generic;

namespace Demo
{
    internal static class DictionaryExtensions
    {
        internal static TValue GetOrCreate<TKey, TValue>(
            this IDictionary<TKey, TValue> dictionary,
            TKey key,
            Func<TValue>? builder = null
        )
            where TValue : new()
        {
            if (!dictionary.TryGetValue(key, out TValue? value))
            {
                value = builder != null ? builder() : new TValue();
                dictionary.Add(key, value);
            }

            return value;
        }
    }
}
