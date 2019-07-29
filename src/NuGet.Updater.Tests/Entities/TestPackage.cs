using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;

namespace NuGet.Updater.Tests.Entities
{
	public class TestPackage : IPackageSearchMetadata
	{
		private string[] _versions;

		public TestPackage(string id, params string[] versions)
		{
			Identity = new PackageIdentity(id, null);
			_versions = versions;
		}

		public string Authors { get; set; }

		public IEnumerable<PackageDependencyGroup> DependencySets { get; set; }

		public string Description { get; set; }

		public long? DownloadCount { get; set; }

		public Uri IconUrl { get; set; }

		public PackageIdentity Identity { get; set; }

		public Uri LicenseUrl { get; set; }

		public Uri ProjectUrl { get; set; }

		public Uri ReportAbuseUrl { get; set; }

		public Uri PackageDetailsUrl { get; set; }

		public DateTimeOffset? Published { get; set; }

		public string Owners { get; set; }

		public bool RequireLicenseAcceptance { get; set; }

		public string Summary { get; set; }

		public string Tags { get; set; }

		public string Title { get; set; }

		public bool IsListed { get; set; }

		public bool PrefixReserved { get; set; }

		public LicenseMetadata LicenseMetadata { get; set; }

		public async Task<IEnumerable<VersionInfo>> GetVersionsAsync() => _versions.Select(v => new VersionInfo(new Versioning.NuGetVersion(v)));
	}
}
