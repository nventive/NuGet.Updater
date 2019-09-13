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
		internal static bool ShouldUpdatePackage(this UpdaterParameters parameters, UpdaterPackage package)
		{
			var isPackageToIgnore = parameters.PackagesToIgnore?.Contains(package.PackageId, StringComparer.OrdinalIgnoreCase) ?? false;
			var isPackageToUpdate = parameters.PackagesToUpdate?.Contains(package.PackageId, StringComparer.OrdinalIgnoreCase) ?? true;

			return isPackageToUpdate && !isPackageToIgnore;
		}

		internal static IEnumerable<string> GetSummary(this UpdaterParameters parameters)
		{
			yield return $"## Configuration";

			var files = parameters.UpdateTarget == UpdateTarget.All
				? string.Join(", ", Enum
					.GetValues(typeof(UpdateTarget))
					.Cast<UpdateTarget>()
					.Select(t => t.GetDescription())
					.Trim()
				)
				: parameters.UpdateTarget.GetDescription();

			yield return $"- Update targeting {files} files under {parameters.SolutionRoot}";

			if(parameters.Sources?.Any() ?? false)
			{
				yield return $"- Using NuGet packages from {string.Join(", ", parameters.Sources.Select(s => s.Url))}";
			}

			if(parameters.PackageAuthor.HasValue())
			{
				yield return $"- Using only public packages authored by {parameters.PackageAuthor}";
			}

			yield return $"- Using {(parameters.Strict ? "exact " : "")}target version {string.Join(", then ", parameters.TargetVersions)}";

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

		public static UpdaterParameters Validate(this UpdaterParameters parameters)
		{
			if(parameters.SolutionRoot.IsNullOrEmpty())
			{
				throw new InvalidOperationException("The solution root must be specified");
			}

			if(parameters.Sources.None())
			{
				throw new InvalidOperationException("At least one NuGet source should be specified");
			}

			return parameters;
		}
	}
}
