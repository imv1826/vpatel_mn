using System;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk;

namespace RSM.Integrations.Dataverse.Common.ExtensionMethods
{
    public static class DictionaryExtensions
    {
        public static void TryAdd<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue value)
        {
            if (!dict.ContainsKey(key))
            {
                dict.Add(key, value);
            }
        }

        public static void AddRange<TKey, TValue>(
            this IDictionary<TKey, TValue> baseDictionary,
            IDictionary<TKey, TValue> dictionaryToAdd
        )
        {
            dictionaryToAdd.ForEach(x => baseDictionary.Add(x.Key, x.Value));
        }

        public static void ForEach<TKey, TValue>(
            this IDictionary<TKey, TValue> source,
            Action<KeyValuePair<TKey, TValue>> action
        )
        {
            foreach (var item in source)
            {
                action(item);
            }
        }

        public static void ForEach(this DataCollection<Entity> source, Action<Entity> action)
        {
            foreach (var item in source)
            {
                action(item);
            }
        }
    }
}