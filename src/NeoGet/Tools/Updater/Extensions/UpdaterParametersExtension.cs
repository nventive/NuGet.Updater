using System;
using System.Collections.Generic;
using System.Linq;
using NuGet.Shared.Entities;
using NuGet.Shared.Extensions;
using NuGet.Shared.Helpers;
using NuGet.Updater.Entities;
using Uno.Extensions;

namespace NuGet.Updater.Extensions
{
	internal static class UpdaterParametersExtension
	{
		internal static IEnumerable<string> GetSummary(this UpdaterParameters parameters)
		{
			yield return $"## Configuration";

			yield return $"- Targeting solution {MarkdownHelper.CodeBlock(parameters.SolutionRoot)}";

			var files = parameters.UpdateTarget == FileType.All
				? Enum
					.GetValues(typeof(FileType))
					.Cast<FileType>()
					.Select(t => t.GetDescription())
					.Trim()
				: new[] { parameters.UpdateTarget.GetDescription() };

			yield return $"- Updating files of type {MarkdownHelper.CodeBlocksEnumeration(files)}";

			if(parameters.Feeds?.Any() ?? false)
			{
				yield return $"- Fetching packages from {MarkdownHelper.CodeBlocksEnumeration(parameters.Feeds.Select(s => s.Url.OriginalString))}";
			}

			if(parameters.PackageAuthor.HasValue())
			{
				yield return $"- Limiting to public packages authored by {MarkdownHelper.Bold(parameters.PackageAuthor)}";
			}

			yield return $"- Using {MarkdownHelper.CodeBlocksEnumeration(parameters.TargetVersions)} versions {(parameters.Strict ? "(exact match)" : "")}";

			if (parameters.IsDowngradeAllowed)
			{
				yield return $"- Downgrading packages if a lower version is found";
			}

			if (parameters.PackagesToUpdate?.Any() ?? false)
			{
				yield return $"- Updating only {MarkdownHelper.CodeBlocksEnumeration(parameters.PackagesToUpdate)}";
			}
		}

		public static UpdaterParameters Validate(this UpdaterParameters parameters)
		{
			if(parameters.SolutionRoot.IsNullOrEmpty())
			{
				throw new InvalidOperationException("The solution root must be specified");
			}

			return parameters;
		}
	}
}
