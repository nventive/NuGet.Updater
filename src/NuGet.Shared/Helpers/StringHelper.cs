using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Uno.Extensions;

namespace NuGet.Shared.Helpers
{
	public static class StringHelper
	{
		internal static string Enumeration(IEnumerable<string> values)
		{
			if(values.None())
			{
				return string.Empty;
			}
			else if(values.Count() == 1)
			{
				return values.First();
			}
			else if(values.Count() == 2)
			{
				return string.Join(" and ", values);
			}
			else
			{
				return $"{string.Join(", ", EnumerableExtensions.SkipLast(values, 1))} and {values.Last()}";
			}
		}
	}
}
