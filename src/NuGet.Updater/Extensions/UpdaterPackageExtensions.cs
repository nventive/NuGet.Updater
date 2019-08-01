using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Updater.Entities;
using Uno.Extensions;

namespace NuGet.Updater.Extensions
{
	public static class UpdaterPackageExtensions
	{
		public static UpdaterVersion GetLatestVersion(this UpdaterPackage package, UpdaterParameters parameters)
		{
			var versionsPerTarget = package
				.AvailableVersions
				.OrderByDescending(v => v)
				.GroupBy(version => parameters.TargetVersions.FirstOrDefault(t => version.IsMatchingVersion(t, parameters.Strict)))
				.Where(g => g.Key.HasValue());

			return versionsPerTarget
				.Select(g => g.FirstOrDefault())
				.OrderByDescending(v => v.Version)
				.FirstOrDefault();
		}
	}
}
