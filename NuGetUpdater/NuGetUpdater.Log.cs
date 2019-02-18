using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Nuget.Updater.Entities;
using NuGet.Versioning;

namespace Nuget.Updater
{
	partial class NuGetUpdater
	{
		private const string LegacyAzureArtifactsFeedUrlPattern = @"https:\/\/(?'account'[^.]*).*_packaging\/(?'feed'[^\/]*)";
		private const string AzureArtifactsFeedUrlPattern = @"https:\/\/pkgs\.dev.azure.com\/(?'account'[^\/]*).*_packaging\/(?'feed'[^\/]*)";

		private static readonly List<UpdateOperation> _updateOperations = new List<UpdateOperation>();

		private static Action<string> _logAction;

		private static void Log(string message) => _logAction(message);

		private static void Log(UpdateOperation operation)
		{
			Log(operation.GetLogMessage());
			_updateOperations.Add(operation);
		}

		private static void LogUpdateSummary(string outputFilePath = null)
		{
			LogSummary(_logAction);

			if (outputFilePath != null)
			{
				LogUpdateSummaryToFile(outputFilePath);
			}
		}

		private static void LogSummary(Action<string> logAction, bool includeUrl = false)
		{
			var completedUpdates = _updateOperations.Where(o => o.ShouldProceed).ToArray();
			var skippedUpdates = _updateOperations.Where(o => !o.ShouldProceed).ToArray();

			if (completedUpdates.Any() || skippedUpdates.Any())
			{
				logAction($"# Package update summary");
			}

			if (completedUpdates.Any())
			{
				var updatedPackages = completedUpdates
					.Select(o => (o.PackageName, o.UpdatedVersion, o.FeedUri))
					.Distinct()
					.ToArray();

				logAction($"## Updated {updatedPackages.Length} packages:");

				foreach (var p in updatedPackages)
				{
					var logMessage = $"[{p.PackageName}] to [{p.UpdatedVersion}]";
					var url = includeUrl ? GetPackageUrl(p.PackageName, p.UpdatedVersion, p.FeedUri) : default;

					logAction(url == null ? $"- {logMessage}" : $"- [{logMessage}]({url})");
				}
			}

			if (skippedUpdates.Any())
			{
				var skippedPackages = skippedUpdates
					.Select(o => (o.PackageName, o.PreviousVersion, o.FeedUri))
					.Distinct()
					.ToArray();

				logAction($"## Skipped {skippedPackages.Length} packages:");

				foreach (var p in skippedPackages)
				{
					var logMessage = $"[{p.PackageName}] is at version [{p.PreviousVersion}]";
					var url = includeUrl ? GetPackageUrl(p.PackageName, p.PreviousVersion, p.FeedUri) : default;

					logAction(url == null ? $"- {logMessage}" : $"- [{logMessage}]({url})");
				}
			}
		}

		private static string GetPackageUrl(string packageId, NuGetVersion version, Uri feedUri)
		{
			if (feedUri.AbsoluteUri.StartsWith("https://api.nuget.org"))
			{
				return $"https://www.nuget.org/packages/{packageId}/{version.ToFullString()}";
			}

			var pattern = LegacyAzureArtifactsFeedUrlPattern;

			if (feedUri.AbsoluteUri.StartsWith("https://pkgs.dev.azure.com"))
			{
				pattern = AzureArtifactsFeedUrlPattern;
			}

			var match = Regex.Match(feedUri.AbsoluteUri, pattern);

			if (match.Length > 0)
			{
				string accountName = match.Groups["account"].Value;
				string feedName = match.Groups["feed"].Value;

				return $"https://dev.azure.com/{accountName}/_packaging?_a=package&feed={feedName}&package={packageId}&version={version.ToFullString()}&protocolType=NuGet";
			}

			return default;
		}
	}
}
