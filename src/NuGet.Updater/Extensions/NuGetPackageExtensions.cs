using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Updater.Entities;
using Uno.Extensions;

namespace NuGet.Updater.Extensions
{
	public static class NuGetPackageExtensions
	{
		public static async Task<UpdaterVersion> GetLatestVersion(
			this NuGetPackage package,
			CancellationToken ct,
			UpdaterParameters parameters
		)
		{
			var versions = await package.GetVersions(ct);

			var versionsPerTarget = versions
				.OrderByDescending(v => v.Version)
				.GroupBy(version => parameters.TargetVersions.FirstOrDefault(t => version.IsMatchingVersion(t, parameters.Strict)))
				.Where(g => g.Key.HasValue());

			return versionsPerTarget
				.Select(g => g.FirstOrDefault())
				.OrderByDescending(v => v.Version)
				.FirstOrDefault();
		}

		private static async Task<IEnumerable<UpdaterVersion>> GetVersions(this NuGetPackage package, CancellationToken ct)
		{
			var versions = new List<UpdaterVersion>();
			foreach(var p in package.Packages)
			{
				foreach(var v in await p.Value.GetVersionsAsync())
				{
					versions.Add(new UpdaterVersion(p.Key, v.Version));
				}
			}

			return versions;
		}
	}
}
