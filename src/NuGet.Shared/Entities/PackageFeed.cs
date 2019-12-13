using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Shared.Extensions;
using Uno.Extensions;

namespace NuGet.Shared.Entities
{
	public class PackageFeed : IPackageFeed
	{
		public static readonly PackageFeed NuGetOrg = new PackageFeed(new PackageSource("https://api.nuget.org/v3/index.json"));

		public static ILogger Logger { get; set; } = ConsoleLogger.Instance;

		public static PackageFeed FromString(string input) => new PackageFeed(input.ToPackageSource());

		private readonly PackageSource _packageSource;

		private PackageFeed(PackageSource source)
		{
			_packageSource = source;
		}

		public Uri Url => _packageSource.SourceUri;

		public bool IsPrivate => _packageSource.Credentials == null;

		public async Task<FeedVersion[]> GetPackageVersions(
			CancellationToken ct,
			PackageReference reference,
			string author = null
		)
		{
			var logMessage = new StringBuilder();

			logMessage.AppendLine($"Retrieving package {reference.Identity.Id} from {Url}");

			var versions = await _packageSource.GetPackageVersions(ct, reference.Identity.Id);

			logMessage.AppendLine(versions.Length > 0 ? $"Found {versions.Length} versions" : "No versions found");

			if(!IsPrivate && author.HasValue() && versions.Any())
			{
				versions = versions.Where(m => m.HasAuthor(author)).ToArray();

				logMessage.AppendLine(versions.Length > 0 ? $"Found {versions.Length} versions from {author}" : $"No versions from {author} found");
			}

			Logger.LogInformation(logMessage.ToString().Trim());

			return versions
				.Select(m => new FeedVersion(m.Version, Url))
				.ToArray();
		}

		public async Task<PackageDependency[]> GetDependencies(
			CancellationToken ct,
			PackageIdentity packageIdentity
		)
		{
			var version = await _packageSource.GetPackageVersion(ct, packageIdentity);

			if(version == null)
			{
				throw new PackageNotFoundException(packageIdentity, Url);
			}

			return version
				.DependencySets
				.SelectMany(g => g.Packages)
				.Distinct()
				.ToArray();
		}

		public async Task<LocalPackage> DownloadPackage(
		   CancellationToken ct,
		   PackageIdentity packageIdentity,
		   string downloadLocation
	   )
		{
			var version = await _packageSource.GetPackageVersion(ct, packageIdentity);

			if (version == null) //Package with this version doesn't exist in the source, skipping.
			{
				return null;
			}

			var downloadResult = await _packageSource.DownloadPackage(ct, packageIdentity, downloadLocation, Logger);

			var localPackagePath = Path.Combine(downloadLocation, $"{packageIdentity}.nupkg");

			File.WriteAllBytes(localPackagePath, downloadResult.PackageStream.ReadBytes());

			return new LocalPackage(packageIdentity, localPackagePath);
		}

		public async Task PushPackage(CancellationToken ct, LocalPackage package)
		{
			var version = await _packageSource.GetPackageVersion(ct, package.Identity);

			if(version != null)
			{
				Logger.LogInformation($"{package.Identity} already exists in source, skipping.");
				return;
			}

			await _packageSource.PushPackage(ct, package, Logger);
		}
	}
}
