using System;
using System.Collections.Generic;
using System.Linq;

namespace NuGet.Updater.Entities
{
	public class UpdaterPackage
	{
		/// <summary>
		/// Creates a new instance of UpdaterPackage from an id, an Url and a list of version. Used for testing.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="sourceUri"></param>
		/// <param name="versions"></param>
		internal UpdaterPackage(string id, Uri sourceUri, params string[] versions)
		{
			PackageId = id;
			AvailableVersions = versions?.Select(v => new UpdaterVersion(v, sourceUri)).ToArray();
		}

		public UpdaterPackage(PackageReference reference, IEnumerable<UpdaterVersion> availableVersions)
			: this(reference)
		{
			AvailableVersions = availableVersions.ToArray();
		}

		public UpdaterPackage(PackageReference reference, UpdaterVersion latestVersion)
			: this(reference)
		{
			LatestVersion = latestVersion;
		}

		private UpdaterPackage(PackageReference reference)
		{
			Reference = reference;
			PackageId = reference.Id;
		}

		public string PackageId { get; }

		public PackageReference Reference { get; set; }

		public UpdaterVersion LatestVersion { get; }

		public UpdaterVersion[] AvailableVersions { get; }
	}
}
