using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Shared.Entities;
using NuGet.Shared.Extensions;
using NuGet.Shared.Helpers;
using NuGet.Updater.Entities;
using NuGet.Updater.Extensions;
using NuGet.Updater.Log;
using Uno.Extensions;

#if WINDOWS_UWP
using XmlDocument = Windows.Data.Xml.Dom.XmlDocument;
#else
using XmlDocument = System.Xml.XmlDocument;
#endif

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("NuGet.Updater.Tests")]

namespace NuGet.Updater
{
	public class NuGetUpdater
	{
		private readonly UpdaterParameters _parameters;
		private readonly UpdaterLogger _log;

		public static async Task<IEnumerable<UpdateResult>> UpdateAsync(
			CancellationToken ct,
			UpdaterParameters parameters,
			TextWriter logWriter = null,
			TextWriter summaryWriter = null
		)
		{
			var updater = new NuGetUpdater(parameters, logWriter, summaryWriter);

			return await updater.UpdatePackages(ct);
		}

		public NuGetUpdater(UpdaterParameters parameters, TextWriter logWriter, TextWriter summaryWriter)
			: this(parameters, new UpdaterLogger(logWriter, summaryWriter))
		{
		}

		internal NuGetUpdater(UpdaterParameters parameters, UpdaterLogger log)
		{
			_parameters = parameters.Validate();
			_log = log;

			PackageFeed.Logger = _log;
		}

		public async Task<IEnumerable<UpdateResult>> UpdatePackages(CancellationToken ct)
		{
			_log.Clear();

			var packages = await GetPackages(ct);
			//Open all the files at once so we don't have to do it all the time
			var documents = await packages
				.Select(p => p.Reference)
				.OpenFiles(ct);

			foreach(var package in packages)
			{
				var version = package.Version;

				if(version == null)
				{
					var targetVersionText = string.Join(" or ", _parameters.TargetVersions);

					if(targetVersionText.HasValue())
					{
						targetVersionText += " ";
					}

					_log.Write($"Skipping [{package.PackageId}]: no {targetVersionText}version found.");
				}
				else
				{
					var operation = package.ToUpdateOperation(_parameters.IsDowngradeAllowed);

					if(version.IsOverride)
					{
						_log.Write($"Version forced to [{operation.UpdatedVersion}] for [{operation.PackageId}]");
					}
					else
					{
						_log.Write($"Latest matching version for [{operation.PackageId}] is [{operation.UpdatedVersion}] on {operation.FeedUri}");
					}

					_log.Write(await UpdateFiles(ct, operation, package.Reference.Files, documents));
				}
			}

			_log.WriteSummary(_parameters);

			return _log.GetResult();
		}

		/// <summary>
		/// Retrieves the NuGet packages according to the set parameters.
		/// </summary>
		/// <param name="ct"></param>
		/// <returns></returns>
		internal async Task<UpdaterPackage[]> GetPackages(CancellationToken ct)
		{
			var packages = new List<UpdaterPackage>();
			var references = await SolutionHelper.GetPackageReferences(ct, _parameters.SolutionRoot, _parameters.UpdateTarget, _log);

			_log.Write($"Found {references.Length} references");

			if(_parameters.Feeds.Any())
			{
				_log.Write($"Retrieving packages from {_parameters.Feeds.Count} feeds");
			}

			foreach(var reference in references.OrderBy(r => r.Identity))
			{
				if(_parameters.PackagesToIgnore.Contains(reference.Identity.Id) ||
					(_parameters.PackagesToUpdate.Any() && !_parameters.PackagesToUpdate.Contains(reference.Identity.Id))
				)
				{
					_log.Write(new UpdateOperation(reference.Identity, isIgnored: true)); 
					continue;
				}

				packages.Add(new UpdaterPackage(reference, await reference.GetLatestVersion(ct, packages, _parameters)));
			}

			return packages.ToArray();
		}

		/// <summary>
		/// Updates a package to the given value in the given files.
		/// </summary>
		/// <param name="ct"></param>
		/// <param name="operation"></param>
		/// <param name="targetFiles"></param>
		/// <returns></returns>
		private async Task<UpdateOperation[]> UpdateFiles(
		   CancellationToken ct,
		   UpdateOperation operation,
		   Dictionary<FileType, string[]> targetFiles,
		   Dictionary<string, XmlDocument> documents
	   )
		{
			var operations = new List<UpdateOperation>();

			foreach(var files in targetFiles)
			{
				var fileType = files.Key;

				foreach(var path in files.Value)
				{
					var document = documents.GetValueOrDefault(path);

					if(document == null)
					{
						continue;
					}

					IEnumerable<UpdateOperation> updates = Array.Empty<UpdateOperation>();

					var currentOperation = operation.WithFilePath(path);

					if(fileType.HasFlag(FileType.Nuspec))
					{
						updates = document.UpdateDependencies(currentOperation);
					}
					else if(fileType.HasAnyFlag(FileType.DirectoryProps, FileType.DirectoryTargets, FileType.Csproj))
					{
						updates = document.UpdatePackageReferences(currentOperation);
					}

					if(!_parameters.IsDryRun && updates.Any(u => u.ShouldProceed()))
					{
						await document.Save(ct, path);
					}

					operations.AddRange(updates);
				}
			}

			return operations.ToArray();
		}
	}
}
