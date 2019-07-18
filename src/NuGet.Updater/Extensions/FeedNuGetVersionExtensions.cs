using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NuGet.Updater.Entities;

namespace NuGet.Updater.Extensions
{
	public static class FeedNuGetVersionExtensions
	{
		public static bool ContainsTag(this FeedNuGetVersion version, string tag) =>
			!string.IsNullOrEmpty(tag)
			&& (version?.Version?.ReleaseLabels?.Contains(tag) ?? false);

		public static bool IsMatchingSpecialVersion(this FeedNuGetVersion version, string specialVersion, bool strict)
		{
			var releaseLabels = version?.Version?.ReleaseLabels;

			if (string.IsNullOrEmpty(specialVersion))
			{
				return !releaseLabels?.Any() ?? true;
			}
			else
			{
				var isMatchingSpecialVersion = releaseLabels?.Any(label => Regex.IsMatch(label, specialVersion, RegexOptions.IgnoreCase)) ?? false;

				return strict
					? releaseLabels?.Count() == 2 && isMatchingSpecialVersion // Check strictly for packages with versions "dev.XXXX"
					: isMatchingSpecialVersion; // Allow packages with versions "dev.XXXX.XXXX"
			}
		}
	}
}
