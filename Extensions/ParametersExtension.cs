using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nuget.Updater.Entities;
using NuGet.Configuration;

namespace Nuget.Updater.Extensions
{
	internal static class ParametersExtension
	{
		internal static bool HasUpdateTarget(this NuGetUpdater.Parameters parameters, UpdateTarget target) => (parameters.UpdateTarget & target) == target;

		internal static PackageSource GetFeedPackageSource(this NuGetUpdater.Parameters parameters) => new PackageSource(parameters.SourceFeed, "Feed")
		{
#if UAP
			Credentials = PackageSourceCredential.FromUserInput("Feed", "user", parameters.SourceFeedPersonalAccessToken, false)
#else
			Credentials = PackageSourceCredential.FromUserInput("Feed", "user", parameters.SourceFeedPersonalAccessToken, false, null)
#endif
		};

		internal static bool ShouldUpdatePackage(this NuGetUpdater.Parameters parameters, NuGetPackage package) =>
			(parameters.PackagesToIgnore == null || !parameters.PackagesToIgnore.Contains(package.PackageId, StringComparer.OrdinalIgnoreCase))
			&& (parameters.PackagesToUpdate == null || parameters.PackagesToUpdate.Contains(package.PackageId, StringComparer.OrdinalIgnoreCase));

		internal static bool ShouldKeepPackageAtLatestDev(this NuGetUpdater.Parameters parameters, string packageId) =>
			parameters.PackagesToKeepAtLatestDev != null && parameters.PackagesToKeepAtLatestDev.Contains(packageId, StringComparer.OrdinalIgnoreCase);

		internal static IEnumerable<string> GetSummary(this NuGetUpdater.Parameters parameters)
		{
			yield return $"## Configuration";

			yield return $"- Update targetting {parameters.SolutionRoot}";

			var packageSources = parameters.IncludeNuGetOrg ? $"NuGet.org and {parameters.SourceFeed}" : parameters.SourceFeed;
			yield return $"- Using NuGet packages from {packageSources}";

			var targetVersion = parameters.UseStableIfMoreRecent ? $"{parameters.TargetVersion} with fallback to stable if a more recent version is available" : parameters.TargetVersion;
			yield return $"- Targeting version {targetVersion}";

			if (parameters.IsDowngradeAllowed)
			{
				yield return $"- Allowing package downgrade if a lower version is found";
			}

			if(parameters.TagToExclude != null && parameters.TagToExclude != "")
			{
				yield return $"- Excluding versions with the {parameters.TagToExclude} tag";
			}

			if(parameters.PackagesToKeepAtLatestDev?.Any() ?? false)
			{
				yield return $"- Keeping {string.Join(",", parameters.PackagesToKeepAtLatestDev)} at latest dev";
			}

			if (parameters.PackagesToIgnore?.Any() ?? false)
			{
				yield return $"- Ignoring {string.Join(",", parameters.PackagesToIgnore)}";
			}

			if (parameters.PackagesToUpdate?.Any() ?? false)
			{
				yield return $"- Updating only {string.Join(",", parameters.PackagesToUpdate)}";
			}
		}
	}
}
