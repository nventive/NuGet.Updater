using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Updater.Entities;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;

namespace NuGet.Updater.Extensions
{
	public static class PackageSourceExtensions
	{
		public static async Task<NuGetPackage[]> SearchPackages(this PackageSource source, CancellationToken ct, Action<string> logAction, string searchTerm = "")
		{
			var settings = Settings.LoadDefaultSettings(null);
			var repositoryProvider = new SourceRepositoryProvider(settings, Repository.Provider.GetCoreV3());

			var repository = repositoryProvider.CreateRepository(source, FeedType.HttpV3);

			logAction($"Pulling NuGet packages from {source.SourceUri}");

			var searchResource = repository.GetResource<PackageSearchResource>();

			var packages = (await searchResource.SearchAsync(searchTerm, new SearchFilter(true, SearchFilterType.IsAbsoluteLatestVersion), skip: 0, take: 1000, log: new NullLogger(), cancellationToken: ct)).ToArray();

			logAction($"Found {packages.Length} packages");

			return source.ToNuGetPackages(packages);
		}

		public static async Task<NuGetPackage[]> ListPackages(this PackageSource source, CancellationToken ct, Action<string> logAction, string searchTerm = "")
		{
			var settings = Settings.LoadDefaultSettings(null);
			var repositoryProvider = new SourceRepositoryProvider(settings, Repository.Provider.GetCoreV3());

			var repository = repositoryProvider.CreateRepository(source, FeedType.HttpV3);

			logAction($"Pulling NuGet packages from {source.SourceUri}");

			var listResource = repository.GetResource<ListResource>();

			var packages = await (await listResource.ListAsync(searchTerm, prerelease: true, allVersions: false, includeDelisted: false, log: new NullLogger(), token: ct)).ToArray();

			logAction($"Found {packages.Length} packages");

			return source.ToNuGetPackages(packages);
		}

		private static NuGetPackage[] ToNuGetPackages(this PackageSource source, IPackageSearchMetadata[] packages) =>
			packages
			.Select(p => new NuGetPackage(p, source.SourceUri))
			.ToArray();
	}
}
