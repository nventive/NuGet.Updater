using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Nuget.Updater.Entities;
using NuGet.Versioning;

namespace Nuget.Updater.Extensions
{
	public static class NuGetPackageExtensions
	{
		public static async Task<FeedNuGetVersion> GetLatestVersion(
			this NuGetPackage package,
			CancellationToken ct,
			string targetVersion,
			string excludeTag,
			bool strict,
			IEnumerable<string> keepLatestDev = null
		)
		{
			var versions = (await package.GetVersions(ct)).OrderByDescending(v => v.Version);


			var specialVersion = targetVersion;

			if ((keepLatestDev?.Contains(package.PackageId, StringComparer.OrdinalIgnoreCase) ?? false))
			{
				specialVersion = "dev";
			}

			if (specialVersion == "stable")
			{
				specialVersion = "";
			}

			var version = versions
				.Where(v => IsMatchingSpecialVersion(specialVersion, v.Version, strict) && !ContainsTag(excludeTag, v.Version))
				.OrderByDescending(v => v.Version)
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

		private static bool ContainsTag(string tag, NuGetVersion version)
		{
			if (tag?.Equals("") ?? true)
			{
				return false;
			}

			return version?.ReleaseLabels?.Contains(tag) ?? false;
		}

		private static bool IsMatchingSpecialVersion(string specialVersion, NuGetVersion version, bool strict)
		{
			if (string.IsNullOrEmpty(specialVersion))
			{
				return !version?.ReleaseLabels?.Any() ?? true;
			}
			else
			{
				var releaseLabels = version?.ReleaseLabels;
				var isMatchingSpecialVersion = releaseLabels?.Any(label => Regex.IsMatch(label, specialVersion, RegexOptions.IgnoreCase)) ?? false;

				return strict
					? releaseLabels?.Count() == 2 && isMatchingSpecialVersion  // Check strictly for packages with versions "dev.XXXX"
					: isMatchingSpecialVersion; // Allow packages with versions "dev.XXXX.XXXX"
			}
		}
	}
}
