using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Packaging.Core;
using NuGet.Shared.Entities;

namespace NuGet.Updater.Tests.Entities
{
	public class TestPackageFeed : IPackageFeed
	{
		private readonly Dictionary<string, string[]> _packages;

		public TestPackageFeed(Uri url, Dictionary<string, string[]> packages)
		{
			Url = url;
			IsPrivate = true;
			_packages = packages;
		}

		public Uri Url { get; }

		public bool IsPrivate { get; }

		public async Task<LocalPackage> DownloadPackage(
			CancellationToken ct,
			PackageIdentity packageIdentity,
			string location
		) => _packages
			.GetValueOrDefault(packageIdentity.Id)
			.Where(v => v.Equals(packageIdentity.Version.ToFullString(), StringComparison.OrdinalIgnoreCase))
			.Select(v => new LocalPackage(packageIdentity, Path.Combine(location, $"{packageIdentity.Id}.nupkg")))
			.SingleOrDefault();

		public async Task<PackageDependency[]> GetDependencies(CancellationToken ct, PackageIdentity packageIdentity) => Array.Empty<PackageDependency>();

		public async Task<FeedVersion[]> GetPackageVersions(
			CancellationToken ct,
			PackageReference reference,
			string author = null
		) => _packages
			.GetValueOrDefault(reference.Identity.Id)
			?.Select(v => new FeedVersion(v, Url))
			.ToArray() ?? new FeedVersion[0];

		public Task<bool> PushPackage(CancellationToken ct, LocalPackage package) => throw new NotSupportedException();
	}
}
