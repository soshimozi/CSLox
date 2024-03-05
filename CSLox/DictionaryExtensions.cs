using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSLox;

internal static class DictionaryExtensions
{
    //public static TValue? Get<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key) where TKey : notnull
    //{
    //    return dict.TryGetValue(key, out var value) ? value : default;
    //}

    public static void Put<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue value) where TKey : notnull
    {
        if(dict.ContainsKey(key)) dict[key] = value;
        else dict.Add(key, value);
    }
}