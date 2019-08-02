using System;
using System.Linq;
using NuGet.Protocol.Core.Types;
using Uno.Extensions;

namespace NuGet.Updater.Extensions
{
	public static class PackageSearchMetadataExtensions
	{
		public static bool HasAuthor(this IPackageSearchMetadata metadata, string author)
		{
			var authors = metadata?.Authors;

			if(authors.IsNullOrEmpty())
			{
				return false;
			}

			return authors.Contains(",")
				? authors.Split(',').Any(a => a.Equals(author, StringComparison.OrdinalIgnoreCase))
				: authors.Equals(author, StringComparison.OrdinalIgnoreCase);
		}
	}
}
