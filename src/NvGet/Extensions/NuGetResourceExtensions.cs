using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NvGet.Entities;

namespace NvGet.Extensions
{
	public static class NuGetResourceExtensions
	{
		public static async Task<PackageSearchMetadataRegistration> GetPackageVersion(
			this PackageMetadataResource resource,
			CancellationToken ct,
			PackageIdentity identity
		)
		{
			var versions = await resource.GetPackageVersions(ct, identity.Id);

			return versions
				.Cast<PackageSearchMetadataRegistration>()
				.FirstOrDefault(m => m.Version.Equals(identity.Version));
		}

		public static async Task<PackageSearchMetadataRegistration[]> GetPackageVersions(
			this PackageMetadataResource resource,
			CancellationToken ct,
			string packageId
		)
		{
			var versions = await resource
				.GetMetadataAsync(packageId, true, false, new SourceCacheContext { NoCache = true }, NullLogger.Instance, ct);

			return versions
				.Cast<PackageSearchMetadataRegistration>()
				.ToArray();
		}

		public static async Task<DownloadResourceResult> DownloadPackage(
			this DownloadResource resource,
			CancellationToken ct,
			PackageIdentity identity,
			string downloadDirectory,
			ILogger log
		)
		{
			var downloadContext = new PackageDownloadContext(new SourceCacheContext { NoCache = true }, downloadDirectory, directDownload: true);

			return await resource
				.GetDownloadResourceResultAsync(identity, downloadContext, downloadDirectory, log, ct);
		}

		public static async Task<bool> PushPackage(
			this PackageUpdateResource resource,
			CancellationToken ct,
			LocalPackage package,
			ILogger log
		)
		{
			await resource.Push(package.Path, null, 100, true, s => s, s => s, true, log);

			return true;
		}

		internal static TResource GetResource<TResource>(this PackageSource source)
			where TResource : class, INuGetResource
		{
			var repositoryProvider = new SourceRepositoryProvider(
				Settings.LoadDefaultSettings(null),
				Repository.Provider.GetCoreV3()
			);

			var repository = repositoryProvider.CreateRepository(source, FeedType.HttpV3);

			return repository.GetResource<TResource>();
		}
	}
}
