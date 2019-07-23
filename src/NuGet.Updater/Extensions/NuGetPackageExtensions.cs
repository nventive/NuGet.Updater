using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Updater.Entities;

namespace NuGet.Updater.Extensions
{
	public static class NuGetPackageExtensions
	{
		public static async Task<FeedNuGetVersion> GetLatestVersion(
			this NuGetPackage package,
			CancellationToken ct,
			UpdaterParameters parameters
		)
		{
			var versions = (await package.GetVersions(ct))
				.OrderByDescending(v => v.Version)
				.ToArray();

			var version = parameters
				.TargetVersions
				.Select(tv =>
				{
					if(tv == "stable")
					{
						tv = "";
					}

					return versions
						.Where(v => v.IsMatchingSpecialVersion(tv, parameters.Strict) && !v.ContainsTag(parameters.TagToExclude))
						.OrderByDescending(v => v.Version)
						.FirstOrDefault();
				}
				)
				.Where(v => v != null)
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
