using System;
using System.Collections.Generic;
using System.Linq;

namespace NeoGet.Extensions
{
	public static class DictionaryExtensions
	{
		public static Dictionary<TKey, TValue> GetItems<TKey, TValue>(
			this IDictionary<TKey, TValue> dictionary,
			params TKey[] keys
		) => dictionary
			.Where(g => keys.Contains(g.Key))
			.ToDictionary(g => g.Key, g => g.Value);

#if !WINDOWS_UWP
		public static bool TryAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
		{
			if(dictionary.ContainsKey(key))
			{
				return false;
			}

			dictionary.Add(key, value);

			return true;
		}

		public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
		{
			if(dictionary.ContainsKey(key))
			{
				return dictionary[key];
			}

			return default;
		}
#endif
	}
}
