using System;
using System.Linq;
using NuGet.Protocol.Core.Types;

namespace NeoGet.Extensions
{
	public static class PackageSearchMetadataExtensions
	{
		public static bool HasAuthor(this IPackageSearchMetadata metadata, string author)
			=> metadata
				?.Authors
				.Split(',')
				.Any(a => a.Equals(author, StringComparison.OrdinalIgnoreCase)) ?? false; 
	}
}
