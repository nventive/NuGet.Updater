using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NvGet.Extensions;
using NvGet.Helpers;
using NvGet.Tools.Updater.Entities;
using NvGet.Tools.Updater.Extensions;
using NuGet.Common;
using NuGet.Versioning;

namespace NvGet.Tools.Updater.Log
{
	public class UpdaterLogger : ILogger, IEqualityComparer<UpdateOperation>
	{
		private readonly List<UpdateOperation> _updateOperations = new List<UpdateOperation>();
		private readonly TextWriter _writer;
		private readonly TextWriter _summaryWriter;

		public UpdaterLogger(TextWriter writer, TextWriter summaryWriter = null)
		{
			_writer = writer
#if DEBUG
				?? Console.Out;
#else
				?? TextWriter.Null;
#endif
			_summaryWriter = summaryWriter;
		}

		public void Clear() => _updateOperations.Clear();

		public IEnumerable<UpdateOperation> GetUpdates() => _updateOperations;

		public void Write(string message) => _writer.WriteLine(message);

		public void Write(IEnumerable<UpdateOperation> operations)
		{
			foreach(var o in operations)
			{
				Write(o);
			}
		}

		public void Write(UpdateOperation operation)
		{
			LogInformation(operation.GetLogMessage());
			_updateOperations.Add(operation);
		}

		public void WriteSummary(UpdaterParameters parameters)
		{
			foreach(var line in GetSummary(parameters, LogDisplayOptions.Console))
			{
				Write(line);
			}

			if(_summaryWriter != null)
			{
				try
				{
					_summaryWriter.Write(string.Join(Environment.NewLine, GetSummary(parameters, LogDisplayOptions.Summary)));
				}
				catch(Exception ex)
				{
					Write($"Failed to write summary. Reason : {ex.Message}");
				}
			}
		}

		public IEnumerable<UpdateResult> GetResult() => _updateOperations
			.Select(o => o.ToUpdateResult())
			.Distinct();

		private IEnumerable<string> GetSummary(UpdaterParameters parameters, LogDisplayOptions options)
		{
			yield return $"# Package update summary";

			if(_updateOperations.Count == 0)
			{
				yield return $"No packages have been updated";
			}

			foreach(var message in LogPackageOperations(_updateOperations, options))
			{
				yield return message;
			}

			foreach(var line in parameters.GetSummary())
			{
				yield return line;
			}
		}

		private IEnumerable<string> LogPackageOperations(IEnumerable<UpdateOperation> operations, LogDisplayOptions options)
		{
			var ignores = operations
				.Where(o => o.IsIgnored)
				.Distinct(this)
				.ToArray();

			if(ignores.Any())
			{
				yield return $"## Ignored {ignores.Length} packages";

				yield return GetOperationsTable(
					ignores,
					new Dictionary<string, Func<UpdateOperation, string>>
					{
						{ "Package", o => o.PackageId },
						{ "Version", o => GetPreviousVersionText(o, options.IncludeUrls) },
					},
					options.PrettifyTable
				);
			}

			var updates = operations
				.Where(o => o.ShouldProceed())
				.Distinct(this)
				.ToArray();

			if(updates.Any())
			{
				yield return $"## Updated {updates.Length} packages";

				yield return GetOperationsTable(
					updates,
					new Dictionary<string, Func<UpdateOperation, string>>
					{
						{ "Package", o => o.PackageId },
						{ "Referenced version", o => GetPreviousVersionText(o, options.IncludeUrls) },
						{ "Updated version", o => GetUpdatedVersionText(o, options.IncludeUrls) },
					},
					options.PrettifyTable
				);
			}

			var skips = operations
				.Where(o => !o.IsIgnored && !o.ShouldProceed())
				.Distinct(this)
				.ToArray();

			if(skips.Any())
			{
				yield return $"## Skipped {skips.Length} packages";

				yield return GetOperationsTable(
					skips,
					new Dictionary<string, Func<UpdateOperation, string>>
					{
						{ "Package", o => o.PackageId },
						{ "Referenced version", o => GetPreviousVersionText(o, options.IncludeUrls) },
						{ "Available version", o => GetUpdatedVersionText(o, options.IncludeUrls) },
					},
					options.PrettifyTable
				);
			}
		}

		private string GetOperationsTable(
			IEnumerable<UpdateOperation> operations,
			Dictionary<string, Func<UpdateOperation, string>> columnBuilder,
			bool prettify
		) => MarkdownHelper.Table(
				columnBuilder.Keys.ToArray(),
				operations.Select(o => columnBuilder.Select(p => p.Value(o)).ToArray()).ToArray(),
				prettify
			);

		private string GetUpdatedVersionText(UpdateOperation operation, bool includeUrl)
			=> GetVersionText(
				operation.UpdatedVersion,
				operation.PackageId,
				operation.FeedUri,
				includeUrl
			);

		private string GetPreviousVersionText(UpdateOperation operation, bool includeUrl)
			=> GetVersionText(
				operation.PreviousVersion,
				operation.PackageId,
				operation.FeedUri,
				includeUrl
			);

		private string GetVersionText(NuGetVersion version, string packageId, Uri feedUri, bool includeUrl)
			=> MarkdownHelper.Link(
				version.OriginalVersion,
				includeUrl ? packageId.GetPackageUrl(version, feedUri) : null
			);

		#region ILogger
		public void LogDebug(string data) => Log(LogLevel.Debug, data);

		public void LogVerbose(string data) => Log(LogLevel.Verbose, data);

		public void LogInformation(string data) => Log(LogLevel.Information, data);

		public void LogMinimal(string data) => Log(LogLevel.Minimal, data);

		public void LogWarning(string data) => Log(LogLevel.Warning, data);

		public void LogError(string data) => Log(LogLevel.Error, data);

		public void LogInformationSummary(string data) => Log(LogLevel.Information, data);

		public void Log(LogLevel level, string data) => Log(new LogMessage(level, data));

		public async Task LogAsync(LogLevel level, string data) => Log(level, data);

		public void Log(ILogMessage message) => Write(message.Message);

		public async Task LogAsync(ILogMessage message) => Log(message);
		#endregion

		#region IEqualityComparer<UpdateOperation>
		public bool Equals(UpdateOperation x, UpdateOperation y)
			=> x != null && y != null
				&& (x.PackageId == y.PackageId && x.PreviousVersion == y.PreviousVersion && x.UpdatedVersion == x.UpdatedVersion);

		public int GetHashCode(UpdateOperation obj) => obj?.PackageId.GetHashCode() ?? 0;
		#endregion
	}
}
