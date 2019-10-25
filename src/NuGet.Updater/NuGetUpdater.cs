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

#if UAP
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

		public static async Task<bool> UpdateAsync(
			CancellationToken ct,
			UpdaterParameters parameters,
			TextWriter logWriter = null,
			string summaryOutputFilePath = null
		)
		{
			var updater = new NuGetUpdater(parameters, logWriter, summaryOutputFilePath);

			return await updater.UpdatePackages(ct);
		}

		public NuGetUpdater(UpdaterParameters parameters, TextWriter logWriter, string summaryOutputFilePath)
			: this(parameters, new UpdaterLogger(logWriter, summaryOutputFilePath))
		{
		}

		internal NuGetUpdater(UpdaterParameters parameters, UpdaterLogger log)
		{
			_parameters = parameters.Validate();
			_log = log;
		}

		public async Task<bool> UpdatePackages(CancellationToken ct)
		{
			_log.Clear();

			var packages = await GetPackages(ct);
			//Open all the files at once so we don't have to do it all the time
			var documents = await packages
				.Select(p => p.Reference)
				.OpenFiles(ct);

			foreach(var package in packages)
			{
				var latestVersion = package.Version;

				if(latestVersion == null)
				{
					_log.Write($"Skipping [{package.PackageId}]: no {string.Join(" or ", _parameters.TargetVersions)} version found.");
				}
				else
				{
					_log.Write($"Latest matching version for [{package.PackageId}] is [{latestVersion.Version}] on {latestVersion.FeedUri}");

					_log.Write(await UpdateFiles(ct, package.PackageId, latestVersion, package.Reference.Files, documents));
				}

				_log.Write("");
			}

			_log.WriteSummary(_parameters);

			return true;
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
			_log.Write("");
			_log.Write($"Retrieving packages from {_parameters.Feeds.Count} feeds");

			foreach(var reference in references.OrderBy(r => r.Identity))
			{
				if(_parameters.PackagesToIgnore.Contains(reference.Identity.Id) ||
					(_parameters.PackagesToUpdate.Any() && !_parameters.PackagesToUpdate.Contains(reference.Identity.Id))
				)
				{
					continue;
				}

				packages.Add(new UpdaterPackage(reference, await reference.GetLatestVersion(ct, _parameters, _log)));

				_log.Write("");
			}

			return packages.ToArray();
		}

		/// <summary>
		/// Updates a package to the given value in the given files.
		/// </summary>
		/// <param name="ct"></param>
		/// <param name="packageId"></param>
		/// <param name="version"></param>
		/// <param name="targetFiles"></param>
		/// <returns></returns>
		private async Task<UpdateOperation[]> UpdateFiles(
		   CancellationToken ct,
		   string packageId,
		   FeedVersion version,
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
					var document = documents[path];
					var updates = new UpdateOperation[0];

					if(fileType.HasFlag(FileType.Nuspec))
					{
						updates = document.UpdateDependencies(packageId, version, path, _parameters.IsDowngradeAllowed);
					}
					else if(fileType.HasAnyFlag(FileType.DirectoryProps, FileType.DirectoryTargets, FileType.Csproj))
					{
						updates = document.UpdatePackageReferences(packageId, version, path, _parameters.IsDowngradeAllowed);
					}

					if(updates.Any(u => u.IsUpdate))
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
