using System;
using System.Collections.Generic;
using System.Linq;
using NuGet.Configuration;
using NuGet.Updater.Entities;
using Uno.Extensions;

namespace NuGet.Updater.Extensions
{
	internal static class UpdaterParametersExtension
	{
		internal static IUpdaterSource[] GetSources(this UpdaterParameters parameters)
		{
			var packageSources = new List<IUpdaterSource>();

			if(parameters.PrivateFeeds?.Any() ?? false)
			{
				packageSources.AddRange(parameters.PrivateFeeds.Select(g => new PrivateUpdaterSource(g.Key, g.Value)));
			}

			if(parameters.IncludeNuGetOrg)
			{
				packageSources.Add(new PublicUpdaterSource("https://api.nuget.org/v3/index.json", parameters.PackagesOwner));
			}

			return packageSources.ToArray();
		}

		internal static bool ShouldUpdatePackage(this UpdaterParameters parameters, NuGetPackage package)
		{
			var isPackageToIgnore = parameters.PackagesToIgnore?.Contains(package.PackageId, StringComparer.OrdinalIgnoreCase) ?? false;
			var isPackageToUpdate = parameters.PackagesToUpdate?.Contains(package.PackageId, StringComparer.OrdinalIgnoreCase) ?? true;

			return isPackageToUpdate && !isPackageToIgnore;
		}

		internal static IEnumerable<string> GetSummary(this UpdaterParameters parameters)
		{
			yield return $"## Configuration";

			yield return $"- Update targetting files under {parameters.SolutionRoot}";

			var packageSources = new List<string>();

			if(parameters.IncludeNuGetOrg)
			{
				packageSources.Add("NuGet.org");
			}

			if(parameters.PrivateFeeds?.Any() ?? false)
			{
				packageSources.AddRange(parameters.PrivateFeeds.Keys);
			}

			yield return $"- Using NuGet packages from {string.Join(", ", packageSources)}";

			yield return $"- Using target version {string.Join(", then ", parameters.TargetVersions)}";

			if (parameters.IsDowngradeAllowed)
			{
				yield return $"- Allowing package downgrade if a lower version is found";
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
