using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NvGet.Contracts;
using NvGet.Entities;
using NvGet.Extensions;
using NvGet.Helpers;
using NvGet.Tools.Updater.Entities;
using Uno.Extensions;

namespace NvGet.Tools.Updater.Extensions
{
	public static class UpdaterParametersExtension
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

			if(parameters.IsDowngradeAllowed)
			{
				yield return $"- Downgrading packages if a lower version is found";
			}

			if(parameters.PackagesToUpdate?.Any() ?? false)
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

		/// <summary>
		/// Gets the latest version for the given reference by looking up first in a list of known packages.
		/// Useful in the cases where refernces to multiple versions of the same packages are found.
		/// </summary>
		public static async Task<FeedVersion> GetLatestVersion(
			this UpdaterParameters parameters,
			CancellationToken ct,
			IEnumerable<UpdaterPackage> knownPackages,
			PackageReference reference
		)
		{
			var knownVersion = knownPackages.FirstOrDefault(p => p.PackageId == reference.Identity.Id)?.Version;

			if(knownVersion == null)
			{
				knownVersion = await parameters.GetLatestVersion(ct, reference);
			}

			return knownVersion;
		}

		public static async Task<FeedVersion> GetLatestVersion(
			this UpdaterParameters parameters,
			CancellationToken ct,
			PackageReference reference
		)
		{
			if(parameters.VersionOverrides.TryGetValue(reference.Identity.Id, out var manualVersion) && manualVersion.forceVersion)
			{
				PackageFeed.Logger.LogInformation($"Overriding version for {reference.Identity.Id}");
				return new FeedVersion(manualVersion.range.MinVersion);
			}

			var availableVersions = await Task.WhenAll(parameters
				.Feeds
				.Select(f => f.GetPackageVersions(ct, reference, parameters.PackageAuthor))
			);

			var versionsPerTarget = availableVersions
				.SelectMany(x => x)
				.Where(v => manualVersion.range?.Satisfies(v.Version) ?? true)
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
