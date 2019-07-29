using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NuGet.Updater.Entities;
using NuGet.Updater.Extensions;
using NuGet.Updater.Helpers;
using Uno.Extensions;

namespace NuGet.Updater.Log
{
	public class Logger
	{
		private readonly List<UpdateOperation> _updateOperations = new List<UpdateOperation>();
		private readonly TextWriter _writer;
		private readonly string _summaryFilePath;

		public Logger(TextWriter writer, string summaryFilePath = null)
		{
			_writer = writer
#if DEBUG
				?? Console.Out;
#else
				?? TextWriter.Null;
#endif
			_summaryFilePath = summaryFilePath;
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
			Write(operation.GetLogMessage());
			_updateOperations.Add(operation);
		}

		public void WriteSummary(UpdaterParameters parameters)
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

			foreach(var message in LogPackageOperations(_updateOperations.Where(o => o.IsUpdate), isUpdate: true, includeUrl))
			{
				yield return message;
			}

			foreach(var message in LogPackageOperations(_updateOperations.Where(o => !o.IsUpdate), isUpdate: false, includeUrl))
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
					name: o.PackageName,
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
	}
}
