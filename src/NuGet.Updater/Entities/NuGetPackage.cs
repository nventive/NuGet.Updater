using System;
using System.Collections.Generic;
using System.Linq;
using NuGet.Protocol.Core.Types;

namespace NuGet.Updater.Entities
{
	public class NuGetPackage
	{
		public NuGetPackage(string packageId, params NuGetPackage[] packages)
		{
			PackageId = packageId;
			Packages = packages
				.SelectMany(p => p.Packages)
				.ToDictionary(p => p.Key, p => p.Value);
		}

		public NuGetPackage(IPackageSearchMetadata package, Uri packageSourceUri)
		{
			PackageId = package.Identity.Id;
			Packages = new Dictionary<Uri, IPackageSearchMetadata>
			{
				{ packageSourceUri, package },
			};
		}

		public string PackageId { get; }

		public Dictionary<Uri, IPackageSearchMetadata> Packages { get; }
	}
}
