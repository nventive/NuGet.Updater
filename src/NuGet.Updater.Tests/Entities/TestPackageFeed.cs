using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
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

		public async Task<FeedVersion[]> GetPackageVersions(
			CancellationToken ct,
			PackageReference reference,
			string author = null,
			ILogger log = null
		) => _packages
			.GetValueOrDefault(reference.Identity.Id)
			?.Select(v => new FeedVersion(v, Url))
			.ToArray() ?? new FeedVersion[0];
	}
}
