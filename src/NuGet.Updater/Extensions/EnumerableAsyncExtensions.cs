using System.Collections.Generic;
using System.Threading.Tasks;
using NuGet.Common;

namespace NuGet.Updater.Extensions
{
	public static class EnumerableAsyncExtensions
	{
		public static async Task<T[]> ToArray<T>(this IEnumerableAsync<T> enumerable)
		{
			var list = new List<T>();
			var enumerator = enumerable.GetEnumeratorAsync();

			while(await enumerator.MoveNextAsync())
			{
				list.Add(enumerator.Current);
			}

			return list.ToArray();
		}
	}
}
