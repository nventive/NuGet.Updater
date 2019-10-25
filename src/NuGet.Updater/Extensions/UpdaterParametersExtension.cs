using System;
using System.Collections.Generic;
using System.Linq;
using NuGet.Shared.Entities;
using NuGet.Shared.Extensions;
using NuGet.Updater.Entities;
using Uno.Extensions;

namespace NuGet.Updater.Extensions
{
	internal static class UpdaterParametersExtension
	{
		internal static IEnumerable<string> GetSummary(this UpdaterParameters parameters)
		{
			yield return $"## Configuration";

			var files = parameters.UpdateTarget == FileType.All
				? string.Join(", ", Enum
					.GetValues(typeof(FileType))
					.Cast<FileType>()
					.Select(t => t.GetDescription())
					.Trim()
				)
				: parameters.UpdateTarget.GetDescription();

			yield return $"- Update targeting {files} files under {parameters.SolutionRoot}";

			if(parameters.Feeds?.Any() ?? false)
			{
				yield return $"- Using NuGet packages from {string.Join(", ", parameters.Feeds.Select(s => s.Url))}";
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

			if(parameters.Feeds.None())
			{
				throw new InvalidOperationException("At least one NuGet feed should be specified");
			}

			return parameters;
		}
	}
}
