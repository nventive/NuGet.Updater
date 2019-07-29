using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NuGet.Updater.Entities;
using Uno.Extensions;

namespace NuGet.Updater.Extensions
{
	public static class UpdaterVersionExtensions
	{
		public static bool IsMatchingVersion(
			this UpdaterVersion version,
			string tag,
			bool isStrict
		)
		{
			var releaseLabels = version?.Version?.ReleaseLabels;

			if(tag.IsNullOrEmpty() || tag == "stable")
			{
				return releaseLabels?.None() ?? true; //Stable versions have no release labels
			}

			var hasTag = ContainsTag(releaseLabels, tag);

			return isStrict
				? releaseLabels?.Count() == 2 && hasTag // Check strictly for packages with versions "dev.XXXX"
				: hasTag; // Allow packages with versions "dev.XXXX.XXXX"
		}

		private static bool ContainsTag(IEnumerable<string> releaseLabels, string tag)
			=> tag.HasValue()
				&& (releaseLabels?.Any(label => Regex.IsMatch(label, tag, RegexOptions.IgnoreCase)) ?? false);
	}
}
