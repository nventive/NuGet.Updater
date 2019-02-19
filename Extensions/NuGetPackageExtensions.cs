using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Nuget.Updater.Entities;
using NuGet.Versioning;

namespace Nuget.Updater.Extensions
{
	public static class NuGetPackageExtensions
	{
		public static async Task<FeedNuGetVersion> GetLatestVersion(
			this NuGetPackage package,
			CancellationToken ct,
			NuGetUpdater.Parameters parameters
		)
		{
			var specialVersion = parameters.TargetVersion;

			if (parameters.ShouldKeepPackageAtLatestDev(package.PackageId))
			{
				specialVersion = "dev";
			}

			if (specialVersion == "stable")
			{
				specialVersion = "";
			}

			var versions = (await package.GetVersions(ct)).OrderByDescending(v => v.Version);

			var version = versions
				.Where(v => v.IsMatchingSpecialVersion(specialVersion, parameters.Strict) && !v.ContainsTag(parameters.TagToExclude))
				.OrderByDescending(v => v.Version)
				.FirstOrDefault();

			return version;
		}

		private static async Task<IEnumerable<FeedNuGetVersion>> GetVersions(this NuGetPackage package, CancellationToken ct)
		{
			var versions = new List<FeedNuGetVersion>();
			foreach(var p in package.Packages)
			{
				foreach(var v in await p.Value.GetVersionsAsync())
				{
					versions.Add(new FeedNuGetVersion(p.Key, v.Version));
				}
			}

			return versions;
		}
	}
}
