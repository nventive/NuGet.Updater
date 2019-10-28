using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Shared.Entities;
using Uno.Extensions;

namespace NuGet.Shared.Extensions
{
	public static class PackageSourceExtensions
	{
		private const char PackageFeedInputSeparator = '|';

		/// <summary>
		/// Transforms a input string into a package feed
		/// If the string matches the {url}|{token} format, a private source will be created.
		/// Otherwise, the input will be used as the URL of a public source.
	 	/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		public static PackageSource ToPackageSource(this string input)
		{
			var parts = input.Split(PackageFeedInputSeparator);

			var url = parts.ElementAtOrDefault(0);
			var accessToken = parts.ElementAtOrDefault(1);

			if(accessToken == null)
			{
				return new PackageSource(url);
			}

			var sourceName = Guid.NewGuid().ToStringInvariant();

			return new PackageSource(url)
			{
#if UAP
				Credentials = PackageSourceCredential.FromUserInput(sourceName, "user", accessToken, false),
#else
				Credentials = PackageSourceCredential.FromUserInput(sourceName, "user", accessToken, false, null),
#endif
			};
		}

		public static async Task<PackageSearchMetadataRegistration> GetPackageVersion(
			this PackageSource source,
			CancellationToken ct,
			PackageIdentity identity
		)
		{
			var versions = await source.GetPackageVersions(ct, identity.Id);

			return versions
				.Cast<PackageSearchMetadataRegistration>()
				.FirstOrDefault(m => m.Version.Equals(identity.Version));
		}

		public static async Task<PackageSearchMetadataRegistration[]> GetPackageVersions(
			this PackageSource source,
			CancellationToken ct,
			string packageId
		)
		{
			var versions = await source
				.GetResource<PackageMetadataResource>()
				.GetMetadataAsync(packageId, true, false, new SourceCacheContext { NoCache = true }, NullLogger.Instance, ct);

			return versions
				.Cast<PackageSearchMetadataRegistration>()
				.ToArray();
		}

		public static async Task<DownloadResourceResult> DownloadPackage(
			this PackageSource source,
			CancellationToken ct,
			PackageIdentity identity,
			string downloadDirectory,
			ILogger log
		)
		{
			var downloadContext = new PackageDownloadContext(new SourceCacheContext { NoCache = true }, downloadDirectory, directDownload: true);

			return await source
				.GetResource<DownloadResource>()
				.GetDownloadResourceResultAsync(identity, downloadContext, downloadDirectory, log, ct);
		}

		public static Task PushPackage(
			this PackageSource source,
			CancellationToken ct,
			LocalPackage package,
			ILogger log
		) => source
			.GetResource<PackageUpdateResource>()
			.Push(package.Path, null, 100, true, s => s, s => s, true, log);

		private static TResource GetResource<TResource>(this PackageSource source)
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
