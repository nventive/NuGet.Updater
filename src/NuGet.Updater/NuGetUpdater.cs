using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Updater.Entities;
using NuGet.Updater.Extensions;
using NuGet.Updater.Helpers;
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
		private readonly Logger _log;

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
			: this(parameters, new Logger(logWriter, summaryOutputFilePath))
		{
		}

		internal NuGetUpdater(UpdaterParameters parameters, Logger log)
		{
			_parameters = parameters.Validate();
			_log = log;
		}

		public async Task<bool> UpdatePackages(CancellationToken ct)
		{
			_log.Clear();

			var packages = await GetPackages(ct);
			//Open all the files at once so we don't have to do it all the time
			var documents = await OpenFiles(ct, packages);

			foreach(var package in packages)
			{
				var latestVersion = package.LatestVersion;

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
			_log.Write($"Retrieving packages from {_parameters.Sources.Count} sources");

			foreach(var reference in references.OrderBy(r => r.Id))
			{
				if(_parameters.PackagesToIgnore.Contains(reference.Id) ||
					(_parameters.PackagesToUpdate.Any() && !_parameters.PackagesToUpdate.Contains(reference.Id))
				)
				{
					continue;
				}

				var matchingPackages = await Task.WhenAll(_parameters
					.Sources
					.Select(source => source.GetPackage(ct, reference, _parameters.PackageAuthor, _log))
				);

				_log.Write("");

				//If the reference has been found on multiple sources, we merge the packages found together
				var mergedPackage = matchingPackages
					.GroupBy(p => p.Reference)
					.Select(g => new UpdaterPackage(g.Key, g.SelectMany(p => p.AvailableVersions)))
					.SingleOrDefault();

				//Retrieve the latest version here so it is only done once per package
				packages.Add(new UpdaterPackage(reference, mergedPackage.GetLatestVersion(_parameters)));
			}

			return packages.ToArray();
		}

		/// <summary>
		/// Opens the XML files where packages were found.
		/// </summary>
		/// <param name="ct"></param>
		/// <param name="packages"></param>
		/// <returns></returns>
		private async Task<Dictionary<string, XmlDocument>> OpenFiles(CancellationToken ct, UpdaterPackage[] packages)
		{
			var files = packages
				.SelectMany(p => p.Reference.Files)
				.SelectMany(g => g.Value)
				.Distinct();

			var documents = new Dictionary<string, XmlDocument>();

			foreach(var file in files)
			{
				documents.Add(file, await file.LoadDocument(ct));
			}

			return documents;
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
		   UpdaterVersion version,
		   Dictionary<UpdateTarget, string[]> targetFiles,
		   Dictionary<string, XmlDocument> documents
	   )
		{
			var operations = new List<UpdateOperation>();

			foreach(var files in targetFiles)
			{
				var updateTarget = files.Key;

				foreach(var path in files.Value)
				{
					var document = documents[path];
					var updates = new UpdateOperation[0];

					if(updateTarget.HasFlag(UpdateTarget.Nuspec))
					{
						updates = document.UpdateDependencies(packageId, version, path, _parameters.IsDowngradeAllowed);
					}
					else if(updateTarget.HasAnyFlag(UpdateTarget.DirectoryProps, UpdateTarget.DirectoryTargets, UpdateTarget.Csproj))
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
