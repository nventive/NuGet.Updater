using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;

namespace NuGet.Shared.Extensions
{
	public static class PackageSourceExtensions
	{
		public static async Task<IPackageSearchMetadata[]> GetPackageVersions(
			this PackageSource source,
			CancellationToken ct,
			string packageId
		)
		{
			var repositoryProvider = new SourceRepositoryProvider(
				Settings.LoadDefaultSettings(null),
				Repository.Provider.GetCoreV3()
			);

			var repository = repositoryProvider.CreateRepository(source, FeedType.HttpV3);

			var packageMetadataResource = await repository.GetResourceAsync<PackageMetadataResource>(ct);

			var versions = await packageMetadataResource.GetMetadataAsync(packageId, true, false, new SourceCacheContext { NoCache = true }, NullLogger.Instance, ct);

			return versions.ToArray();
		}
	}
}
