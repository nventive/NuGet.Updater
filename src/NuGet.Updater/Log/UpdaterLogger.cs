using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Updater.Entities;
using NuGet.Updater.Extensions;
using NuGet.Updater.Helpers;
using Uno.Extensions;

namespace NuGet.Updater.Log
{
	public class UpdaterLogger : ILogger
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

		internal IEnumerable<UpdateOperation> GetUpdates() => _updateOperations;

		public void Write(string message) => _writer.WriteLine(message);

		public void Write(IEnumerable<UpdateOperation> operations)
		{
			foreach (var o in operations)
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
			foreach (var line in GetSummary(parameters))
			{
				Write(line);
			}

			if (_summaryWriter != null)
			{
				try
				{
					_summaryWriter.Write(string.Join(Environment.NewLine, GetSummary(parameters, includeUrl: true)));
				}
				catch (Exception ex)
				{
					Write($"Failed to write summary. Reason : {ex.Message}");
				}
			}
		}

		public IEnumerable<UpdateResult> GetResult() => _updateOperations.Where(o => o.ShouldProceed).Select(o => o.ToUpdateResult()).Distinct();

		private IEnumerable<string> GetSummary(UpdaterParameters parameters, bool includeUrl = false)
		{
			yield return $"# Package update summary";

			if (_updateOperations.Count == 0)
			{
				yield return $"No packages have been updated.";
			}

			foreach(var line in parameters.GetSummary())
			{
				yield return line;
			}

			foreach(var message in LogPackageOperations(_updateOperations.Where(o => o.ShouldProceed), isUpdate: true, includeUrl))
			{
				yield return message;
			}

			foreach(var message in LogPackageOperations(_updateOperations.Where(o => !o.ShouldProceed), isUpdate: false, includeUrl))
			{
				yield return message;
			}
		}

		private IEnumerable<string> LogPackageOperations(IEnumerable<UpdateOperation> operations, bool isUpdate, bool includeUrl)
		{
			if(operations.None())
			{
				yield break;
			}

			var packages = operations
				.Select(o => (
					name: o.PackageId,
					version: isUpdate ? o.UpdatedVersion : o.PreviousVersion,
					uri: o.FeedUri
				))
				.Distinct()
				.ToArray();

			yield return $"## {(isUpdate ? "Updated" : "Skipped")} {packages.Length} packages:";

			foreach(var p in packages)
			{
				var logMessage = $"[{p.name}] {(isUpdate ? "to" : "is at version")} [{p.version}]";

				var url = includeUrl ? PackageHelper.GetUrl(p.name, p.version, p.uri) : default;

				yield return url == null ? $"- {logMessage}" : $"- [{logMessage}]({url})";
			}
		}

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
	}
}
