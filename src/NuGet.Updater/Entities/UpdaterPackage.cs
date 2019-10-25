using NuGet.Shared.Entities;

namespace NuGet.Updater.Entities
{
	public class UpdaterPackage
	{
		public UpdaterPackage(PackageReference reference, FeedVersion version)
		{
			Reference = reference;
			PackageId = reference.Identity.Id;
			Version = version;
		}

		public string PackageId { get; }

		public PackageReference Reference { get; }

		public FeedVersion Version { get; }
	}
}
