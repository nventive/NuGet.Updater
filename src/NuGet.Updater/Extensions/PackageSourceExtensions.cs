using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Updater.Entities;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Updater.Log;

namespace NuGet.Updater.Extensions
{
	public static class PackageSourceExtensions
	{
		public static async Task<UpdaterPackage[]> SearchPackages(this PackageSource source, CancellationToken ct, string searchTerm = "", Logger log = null)
		{
			var settings = Settings.LoadDefaultSettings(null);
			var repositoryProvider = new SourceRepositoryProvider(settings, Repository.Provider.GetCoreV3());

			var repository = repositoryProvider.CreateRepository(source, FeedType.HttpV3);

			log?.Write($"Pulling packages from {source.SourceUri}");

			var searchResource = repository.GetResource<PackageSearchResource>();

			var packages = (await searchResource.SearchAsync(searchTerm, new SearchFilter(true, SearchFilterType.IsAbsoluteLatestVersion), skip: 0, take: 1000, log: new NullLogger(), cancellationToken: ct)).ToArray();

			log?.Write($"Found {packages.Length} packages");

			return source.ToNuGetPackages(packages);
		}

		public static async Task<UpdaterPackage> GetPackage(this PackageSource source, CancellationToken ct, PackageReference reference, Logger log = null)
		{
			var settings = Settings.LoadDefaultSettings(null);
			var repositoryProvider = new SourceRepositoryProvider(settings, Repository.Provider.GetCoreV3());

			var repository = repositoryProvider.CreateRepository(source, FeedType.HttpV3);

			var packageId = reference.Id;

			log?.Write($"Getting package {packageId} from {source.SourceUri}");

			var resource = repository.GetResource<PackageMetadataResource>();

			var metadata = await resource.GetMetadataAsync(packageId, true, false, new SourceCacheContext(), new NullLogger(), ct);

			var versions = metadata
				.Cast<PackageSearchMetadataRegistration>()
				.Select(m => new UpdaterVersion(source.SourceUri, m.Version))
				.ToArray();

			return new UpdaterPackage(packageId, versions, source.SourceUri, reference);
		}

		private static UpdaterPackage[] ToNuGetPackages(this PackageSource source, IPackageSearchMetadata[] packages) =>
			packages
			.Select(p => new UpdaterPackage(p, source.SourceUri))
			.ToArray();
	}
}
