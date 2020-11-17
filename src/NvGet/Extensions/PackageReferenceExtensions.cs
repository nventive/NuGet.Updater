using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NvGet.Entities;

namespace NvGet.Extensions
{
	public static class PackageReferenceExtensions
	{
		public static IEnumerable<PackageReferenceHolder> GetReferenceHolders(this IEnumerable<PackageReference> references)
			=> references
				.SelectMany(r => r
					.Files
					.SelectMany(p => p.Value)
					.Select(f => new { r.Identity, File = f })
				)
				.GroupBy(a => a.File)
				.Select(g => new PackageReferenceHolder(g.Key, g.Select(a => a.Identity)));
	}
}
