using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using NvGet.Contracts;
using NvGet.Extensions;
using Uno.Extensions;
using NuGet.Shared.Extensions;
using NuGet.Versioning;

namespace NvGet.Entities
{
	public class PackageFeed : IPackageFeed
	{
		public static readonly PackageFeed NuGetOrg = new PackageFeed(new PackageSource("https://api.nuget.org/v3/index.json"));

		public static ILogger Logger { get; set; } = ConsoleLogger.Instance;

		public static PackageFeed FromString(string input) => new PackageFeed(input.ToPackageSource());

		private readonly Lazy<PackageMetadataResource> _packageSearchMetadata;
		private readonly Lazy<DownloadResource> _downloadResource;
		private readonly Lazy<PackageUpdateResource> _packageUpdateResource;

		private PackageFeed(PackageSource source)
		{
			Url = source.SourceUri;
			IsPrivate = source.Credentials != null;

			_packageSearchMetadata = new(() => source.GetResource<PackageMetadataResource>());
			_downloadResource = new(() => source.GetResource<DownloadResource>());
			_packageUpdateResource = new(() => source.GetResource<PackageUpdateResource>());
		}

		public Uri Url { get; }

		public bool IsPrivate { get; }

		public async Task<NuGetVersion[]> GetPackageVersions(
			CancellationToken ct,
			PackageReference reference,
			string author = null
		)
		{
			var logMessage = new StringBuilder();

			logMessage.AppendLine($"Retrieving package {reference.Identity.Id} from {Url}");

			var versions = await _packageSearchMetadata.Value.GetPackageVersions(ct, reference.Identity.Id);

			logMessage.AppendLine(versions.Length > 0 ? $"Found {versions.Length} versions" : "No versions found");

			if(!IsPrivate && author.HasValue() && versions.Any())
			{
				versions = versions.Where(m => m.HasAuthor(author)).ToArray();

				logMessage.AppendLine(versions.Length > 0 ? $"Found {versions.Length} versions from {author}" : $"No versions from {author} found");
			}

			Logger.LogInformation(logMessage.ToString().Trim());

			return versions
				.Select(m => m.Version)
				.ToArray();
		}

		public async Task<Dictionary<NuGetFramework, PackageDependency[]>> GetDependencies(
			CancellationToken ct,
			PackageIdentity packageIdentity
		)
		{
			var version = await _packageSearchMetadata.Value.GetPackageVersion(ct, packageIdentity);

			if(version == null)
			{
				throw new PackageNotFoundException(packageIdentity, Url);
			}

			return version
				.DependencySets
				.ToDictionary(g => g.TargetFramework, g => g.Packages.ToArray());
		}

		public async Task<LocalPackage> DownloadPackage(
		   CancellationToken ct,
		   PackageIdentity packageIdentity,
		   string downloadLocation
	   )
		{
			var version = await _packageSearchMetadata.Value.GetPackageVersion(ct, packageIdentity);

			if (version == null) //Package with this version doesn't exist in the source, skipping.
			{
				return null;
			}

			var downloadResult = await _downloadResource.Value.DownloadPackage(ct, packageIdentity, downloadLocation, Logger);

			var localPackagePath = Path.Combine(downloadLocation, $"{packageIdentity}.nupkg");

			File.WriteAllBytes(localPackagePath, downloadResult.PackageStream.ReadBytes());

			return new LocalPackage(packageIdentity, localPackagePath);
		}

		public async Task<bool> PushPackage(CancellationToken ct, LocalPackage package)
		{
			var version = await _packageSearchMetadata.Value.GetPackageVersion(ct, package.Identity);

			if(version != null)
			{
				Logger.LogInformation($"{package.Identity} already exists in source, skipping");
				return false;
			}

			return await _packageUpdateResource.Value.PushPackage(ct, package, Logger);
		}

		public override int GetHashCode() => Url.GetHashCode();

		public override bool Equals(object obj)
		{
			if(obj is PackageFeed other)
			{
				if(Url != null && other?.Url != null)
				{
					return this.Url.Equals(other.Url);
				}
			}

			return base.Equals(obj);
		}
	}
}
