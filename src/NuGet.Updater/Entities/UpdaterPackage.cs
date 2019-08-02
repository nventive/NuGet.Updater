using System;
using System.Linq;

namespace NuGet.Updater.Entities
{
	public class UpdaterPackage
	{
		public UpdaterPackage(PackageReference reference, params UpdaterVersion[] versions)
			: this(reference.Id, versions)
		{
			Reference = reference;
		}

		internal UpdaterPackage(string id, Uri sourceUri, params string[] versions)
			: this(id, versions?.Select(v => new UpdaterVersion(v, sourceUri)).ToArray())
		{
		}

		private UpdaterPackage(string id, params UpdaterVersion[] versions)
		{
			PackageId = id;
			AvailableVersions = versions;
		}

		public string PackageId { get; }

		public PackageReference Reference { get; set; }

		public UpdaterVersion[] AvailableVersions { get; }
	}
}
