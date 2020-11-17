using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Frameworks;
using NuGet.Packaging.Core;
using NvGet.Contracts;
using NvGet.Entities;

namespace NvGet.Tools.Tests.Entities
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

		public async Task<Dictionary<NuGetFramework, PackageDependency[]>> GetDependencies(CancellationToken ct, PackageIdentity packageIdentity) => new Dictionary<NuGetFramework, PackageDependency[]>();

		public async Task<FeedVersion[]> GetPackageVersions(
			CancellationToken ct,
			PackageReference reference,
			string author = null
		) => _packages
			.GetValueOrDefault(reference.Identity.Id)
			?.Select(v => new FeedVersion(v, Url))
			.ToArray() ?? Array.Empty<FeedVersion>();

		public Task<bool> PushPackage(CancellationToken ct, LocalPackage package) => throw new NotSupportedException();
	}
}
