using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Updater.Entities;
using NuGet.Updater.Log;

namespace NuGet.Updater.Tests.Entities
{
	public class TestPackageSource : IUpdaterSource
	{
		private readonly Dictionary<string, string[]> _packages;

		public TestPackageSource(Uri url, Dictionary<string, string[]> packages)
		{
			Url = url;
			_packages = packages;
		}

		public Uri Url { get; }

		public async Task<UpdaterPackage> GetPackage(CancellationToken ct, PackageReference reference, string author, Logger log = null)
		{
			var packageId = reference.Id;

			var versions = _packages
				.GetValueOrDefault(reference.Id)
				?.Select(v => new UpdaterVersion(v, Url))
				.ToArray() ?? new UpdaterVersion[0];

			return new UpdaterPackage(reference, versions);
		}
	}
}
