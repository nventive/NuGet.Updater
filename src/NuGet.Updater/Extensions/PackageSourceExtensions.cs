using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Updater.Entities;
using NuGet.Updater.Log;
using Uno.Extensions;

namespace NuGet.Updater.Extensions
{
	public static class PackageSourceExtensions
	{
		public static async Task<UpdaterPackage> GetPackage(
			this PackageSource source,
			CancellationToken ct,
			PackageReference reference,
			string author = null,
			Logger log = null
		)
		{
			var repositoryProvider = new SourceRepositoryProvider(
				Settings.LoadDefaultSettings(null),
				Repository.Provider.GetCoreV3()
			);

			var repository = repositoryProvider.CreateRepository(source, FeedType.HttpV3);

			var packageId = reference.Id;

			log?.Write($"Retrieving package {packageId} from {source.SourceUri}");

			var metadata = (await repository
				.GetResource<PackageMetadataResource>()
				.GetMetadataAsync(packageId, true, false, new SourceCacheContext { NoCache = true }, new NullLogger(), ct))
				.ToArray();

			log?.Write(metadata.Length > 0 ? $"Found {metadata.Length} versions" : "No versions found");

			if(author.HasValue() && metadata.Any())
			{
				metadata = metadata.Where(m => m.HasAuthor(author)).ToArray();

				log?.Write(metadata.Length > 0 ? $"Found {metadata.Length} version from {author}" : $"No versions from {author} found");
			}

			var versions = metadata
				.Cast<PackageSearchMetadataRegistration>()
				.Select(m => new UpdaterVersion(m.Version, source.SourceUri))
				.ToArray();

			return new UpdaterPackage(reference, versions);
		}
	}
}
