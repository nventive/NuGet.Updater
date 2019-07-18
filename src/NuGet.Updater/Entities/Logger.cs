using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NuGet.Updater.Extensions;
using NuGet.Updater.Helpers;
using NuGet.Versioning;

namespace NuGet.Updater.Entities
{
	public class Logger
	{
		private const string LegacyAzureArtifactsFeedUrlPattern = @"https:\/\/(?'account'[^.]*).*_packaging\/(?'feed'[^\/]*)";
		private const string AzureArtifactsFeedUrlPattern = @"https:\/\/pkgs\.dev.azure.com\/(?'account'[^\/]*).*_packaging\/(?'feed'[^\/]*)";

		private readonly List<UpdateOperation> _updateOperations = new List<UpdateOperation>();
		private readonly TextWriter _writer;
		private readonly string _summaryFilePath;

		public Logger(TextWriter writer, string summaryFilePath = null)
		{
			writer = writer
#if DEBUG
				?? Console.Out;
#else
				?? TextWriter.Null;
#endif
			_summaryFilePath = summaryFilePath;
		}

		public void Clear() => _updateOperations.Clear();

		public void Write(string message) => _writer.Write(message);

		public void Write(IEnumerable<UpdateOperation> operations)
		{
			foreach (var o in operations)
			{
				Write(o);
			}
		}

		public void Write(UpdateOperation operation)
		{
			Write(operation.GetLogMessage());
			_updateOperations.Add(operation);
		}

		public void WriteSummary(NuGetUpdater.Parameters parameters)
		{
			foreach (var line in GetSummary(parameters))
			{
				Write(line);
			}

			if (_summaryFilePath != null)
			{
				try
				{
					FileHelper.LogToFile(_summaryFilePath, GetSummary(parameters, includeUrl: true));
				}
				catch (Exception ex)
				{
					Write($"Failed to write to {_summaryFilePath}. Reason : {ex.Message}");
				}
			}
		}

		private IEnumerable<string> GetSummary(NuGetUpdater.Parameters parameters, bool includeUrl = false)
		{
			var completedUpdates = _updateOperations.Where(o => o.ShouldProceed).ToArray();
			var skippedUpdates = _updateOperations.Where(o => !o.ShouldProceed).ToArray();

			yield return $"# Package update summary";

			if (_updateOperations.Count == 0)
			{
				yield return $"No packages have been updated.";
			}

			foreach(var line in parameters.GetSummary())
			{
				yield return line;
			}

			if (completedUpdates.Any())
			{
				var updatedPackages = completedUpdates
					.Select(o => (o.PackageName, o.UpdatedVersion, o.FeedUri))
					.Distinct()
					.ToArray();

				yield return $"## Updated {updatedPackages.Length} packages:";

				foreach (var p in updatedPackages)
				{
					var logMessage = $"[{p.PackageName}] to [{p.UpdatedVersion}]";
					var url = includeUrl ? GetPackageUrl(p.PackageName, p.UpdatedVersion, p.FeedUri) : default;

					yield return url == null ? $"- {logMessage}" : $"- [{logMessage}]({url})";
				}
			}

			if (skippedUpdates.Any())
			{
				var skippedPackages = skippedUpdates
					.Select(o => (o.PackageName, o.PreviousVersion, o.FeedUri))
					.Distinct()
					.ToArray();

				yield return $"## Skipped {skippedPackages.Length} packages:";

				foreach (var p in skippedPackages)
				{
					var logMessage = $"[{p.PackageName}] is at version [{p.PreviousVersion}]";
					var url = includeUrl ? GetPackageUrl(p.PackageName, p.PreviousVersion, p.FeedUri) : default;

					yield return url == null ? $"- {logMessage}" : $"- [{logMessage}]({url})";
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
