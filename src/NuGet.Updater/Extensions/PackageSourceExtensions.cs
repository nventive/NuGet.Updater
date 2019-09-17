using System.Linq;
using System.Text;
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
			var logMessage = new StringBuilder();

			var repositoryProvider = new SourceRepositoryProvider(
				Settings.LoadDefaultSettings(null),
				Repository.Provider.GetCoreV3()
			);

			var repository = repositoryProvider.CreateRepository(source, FeedType.HttpV3);

			var packageId = reference.Id;

			logMessage.AppendLine($"Retrieving package {packageId} from {source.SourceUri}");

			var packageMetadata = (await repository
				.GetResource<PackageMetadataResource>()
				.GetMetadataAsync(packageId, true, false, new SourceCacheContext { NoCache = true }, new NullLogger(), ct))
				.ToArray();

			logMessage.AppendLine(packageMetadata.Length > 0 ? $"Found {packageMetadata.Length} versions" : "No versions found");

			if(author.HasValue() && packageMetadata.Any())
			{
				packageMetadata = packageMetadata.Where(m => m.HasAuthor(author)).ToArray();

				logMessage.AppendLine(packageMetadata.Length > 0 ? $"Found {packageMetadata.Length} versions from {author}" : $"No versions from {author} found");
			}

			var versions = packageMetadata
				.Cast<PackageSearchMetadataRegistration>()
				.Select(m => new UpdaterVersion(m.Version, source.SourceUri))
				.ToArray();

			log?.Write(logMessage.ToString());

			return new UpdaterPackage(reference, versions);
		}
	}
}
