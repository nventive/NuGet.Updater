using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NuGet.Versioning;
using Uno.Extensions;

namespace NvGet.Extensions
{
	public static class StringExtensions
	{
		private const string LegacyAzureArtifactsFeedUrlPattern = @"https:\/\/(?'account'[^.]*).*_packaging\/(?'feed'[^\/]*)";
		private const string AzureArtifactsFeedUrlPattern = @"https:\/\/pkgs\.dev.azure.com\/(?'account'[^\/]*).*_packaging\/(?'feed'[^\/]*)";

		public static string GetPackageUrl(this string packageId, NuGetVersion version, Uri feedUri)
		{
			if(feedUri == null)
			{
				return default;
			}

			if(feedUri.AbsoluteUri.StartsWith("https://api.nuget.org", StringComparison.OrdinalIgnoreCase))
			{
				return $"https://www.nuget.org/packages/{packageId}/{version.ToFullString()}";
			}

			var pattern = LegacyAzureArtifactsFeedUrlPattern;

			if(feedUri.AbsoluteUri.StartsWith("https://pkgs.dev.azure.com", StringComparison.OrdinalIgnoreCase))
			{
				pattern = AzureArtifactsFeedUrlPattern;
			}

			var match = Regex.Match(feedUri.AbsoluteUri, pattern);

			if(match.Length > 0)
			{
				string accountName = match.Groups["account"].Value;
				string feedName = match.Groups["feed"].Value;

				return $"https://dev.azure.com/{accountName}/_packaging?_a=package&feed={feedName}&package={packageId}&version={version.ToFullString()}&protocolType=NuGet";
			}

			return default;
		}

		public static string GetEnumeration(this IEnumerable<string> values)
		{
			if(values.None())
			{
				return string.Empty;
			}
			else if(values.Count() == 1)
			{
				return values.First();
			}
			else if(values.Count() == 2)
			{
				return string.Join(" and ", values);
			}
			else
			{
				return $"{string.Join(", ", EnumerableExtensions.SkipLast(values, 1))} and {values.Last()}";
			}
		}
	}
}
