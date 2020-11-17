using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Versioning;
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
			var manualVersion = parameters.VersionOverrides.FirstOrDefault(v => v.IsFixedVersion);
			
			if(manualVersion?.IsFixedVersion ?? false)
			{
				PackageFeed.Logger.LogInformation($"Overriding version for {reference.Identity.Id}");
				return new FeedVersion(manualVersion.Version);
			}

			var targetVersionTags = (manualVersion?.VersionTag).SelectOrDefault(t => new[] { t }, parameters.TargetVersions);

			Dictionary<Uri, NuGetVersion[]> availableVersions = new();

			foreach(var feed in parameters.Feeds)
			{
				availableVersions.Add(feed.Url, await feed.GetPackageVersions(ct, reference, parameters.PackageAuthor));
			}

			var versionsPerTarget = availableVersions
				.Select(x => x
					.Value
					.Where(v => manualVersion.Range?.Satisfies(v) ?? true)
					.GroupBy(v => targetVersionTags.FirstOrDefault(t => v.IsMatchingVersion(t, parameters.Strict)))
					.Where(g => g.Key.HasValue())
					.SelectMany(g => g
						.OrderByDescending(v => v)
						.Select(v => new FeedVersion(v, feedUri: x.Key, versionTag: g.Key))
					)
				);

			return versionsPerTarget
				.Select(g => g.FirstOrDefault())
				.OrderByDescending(v => v.Version)
				.FirstOrDefault();
		}
	}
}
