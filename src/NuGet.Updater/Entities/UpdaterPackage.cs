using System;
using System.Collections.Generic;
using NuGet.Protocol.Core.Types;

namespace NuGet.Updater.Entities
{
	public class UpdaterPackage
	{
		public UpdaterPackage(IPackageSearchMetadata version, Uri sourceUri)
		{
			PackageId = version.Identity.Id;
			//Versions = new[] { version };
			SourceUri = sourceUri;

			Packages = new Dictionary<Uri, IPackageSearchMetadata>
			{
				{ sourceUri, version },
			};
		}

		public UpdaterPackage(string id, UpdaterVersion[] versions, Uri sourceUri, PackageReference reference)
		{
			PackageId = id;
			AvailableVersions = versions;
			SourceUri = sourceUri;
			Reference = reference;
		}

		public Uri SourceUri { get; set; }

		public string PackageId { get; }

		public PackageReference Reference { get; set; }

		public UpdaterVersion[] AvailableVersions { get; }

		//TO Remove
		public Dictionary<Uri, IPackageSearchMetadata> Packages { get; }
	}
}
