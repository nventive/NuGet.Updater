using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Updater.Entities;
using NuGet.Updater.Log;

namespace NuGet.Updater.Tests.Entities
{
	public class TestPackageSource : IUpdaterSource
	{
		public TestPackageSource(Uri uri, params TestPackage[] packages)
		{
			Uri = uri;
			//Packages = packages?.Select(p => new NuGetPackage(p, Uri)).ToArray();
		}

		public Uri Uri { get; }

		public UpdaterPackage[] Packages { get; }

		public async Task<UpdaterPackage> GetPackage(CancellationToken ct, PackageReference reference, Logger log = null) => Packages.FirstOrDefault(p => p.PackageId == reference.Id);

		public async Task<UpdaterPackage[]> GetPackages(CancellationToken ct, Logger log = null) => Packages;
	}
}
