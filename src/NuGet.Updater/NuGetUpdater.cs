using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Updater.Entities;
using NuGet.Updater.Extensions;
using NuGet.Updater.Helpers;
using NuGet.Versioning;
using Uno.Extensions;
using NuGet.Updater.Log;
using System;

#if UAP
using XmlDocument = Windows.Data.Xml.Dom.XmlDocument;
#else
using XmlDocument = System.Xml.XmlDocument;
#endif

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("NuGet.Updater.Tests")]

namespace NuGet.Updater
{
	/// <summary>
	/// NuGet Updater implementation.
	/// </summary>
	public partial class NuGetUpdater
	{
		private readonly UpdaterParameters _parameters;
		private readonly Logger _log;
		private readonly IUpdaterSource[] _packageSources;

		internal NuGetUpdater(UpdaterParameters parameters, IUpdaterSource[] packageSources, Logger log)
		{
			//TODO validate parameters
			_parameters = parameters;
			_log = log;
			_packageSources = packageSources;
		}

		internal async Task<bool> UpdatePackages(CancellationToken ct)
		{
			_log.Clear();

			var packages = await GetPackages(ct);
			var documents = await OpenFiles(ct, packages);

			foreach(var package in packages.Where(p => _parameters.ShouldUpdatePackage(p)))
			{
				var latest = package.GetLatestVersion(_parameters);

				if(latest == null)
				{
					_log.Write($"Skipping [{package.PackageId}]: no {string.Join(" or ", _parameters.TargetVersions)} version found.");
					continue;
				}

				_log.Write($"Latest matching version for [{package.PackageId}] is [{latest.Version}] on {package.SourceUri}");

				var updates = new UpdateOperation[0];

				foreach(var files in package.Reference.Files)
				{
					switch(files.Key)
					{
						case var t when t.Matches(UpdateTarget.Nuspec):
							updates = await UpdateNuSpecs(ct, package.PackageId, latest, documents.GetItems(files.Value), _parameters.IsDowngradeAllowed);
							break;
						case var t when t.Matches(UpdateTarget.ProjectJson):
							updates = await UpdateProjectJson(ct, package.PackageId, latest, files.Value, _parameters.IsDowngradeAllowed);
							break;
						case var t when t.Matches(UpdateTarget.DirectoryProps, UpdateTarget.DirectoryTargets, UpdateTarget.Csproj):
							updates = await UpdateProjects(ct, package.PackageId, latest, documents.GetItems(files.Value), _parameters.IsDowngradeAllowed);
							break;
						default:
							break;
					}
				}

				_log.Write(updates);

				_log.Write("");
			}

			_log.WriteSummary(_parameters);

			return true;
		}

		internal async Task<UpdaterPackage[]> GetPackages(CancellationToken ct)
		{
			var packages = new List<UpdaterPackage>();
			var references = await SolutionHelper.GetPackageReferences(ct, _parameters.SolutionRoot, _parameters.UpdateTarget);

			_log.Write($"Found {references.Length} references");

			foreach(var reference in references.OrderBy(r => r.Id))
			{
				foreach(var source in _packageSources)
				{
					packages.Add(await source.GetPackage(ct, reference, _log));
				}
			}

			return packages
				.Where(p => p.AvailableVersions?.Any() ?? false)
				.ToArray();
		}

		private async Task<Dictionary<string, XmlDocument>> OpenFiles(CancellationToken ct, UpdaterPackage[] packages)
		{
			var files = packages
				.SelectMany(p => p.Reference.Files)
				.SelectMany(g => g.Value)
				.Distinct();

			var documents = new Dictionary<string, XmlDocument>();

			foreach(var file in files)
			{
				documents.Add(file, await file.GetDocument(ct));
			}

			return documents;
		}

		private static async Task<UpdateOperation[]> UpdateNuSpecs(
			CancellationToken ct,
			string packageId,
			UpdaterVersion version,
			Dictionary<string, XmlDocument> nuspecDocuments,
			bool isDowngradeAllowed
		)
		{
			var operations = new List<UpdateOperation>();

			foreach(var document in nuspecDocuments)
			{
				var path = document.Key;
				var xmlDocument = document.Value;

				var updates = xmlDocument.UpdateNuSpecVersions(packageId, version, path, isDowngradeAllowed);

				if(updates.Any(u => u.IsUpdate))
				{
					await xmlDocument.Save(ct, path);
				}

				operations.AddRange(updates);
			}

			return operations.ToArray();
		}

		private static async Task<UpdateOperation[]> UpdateProjects(
			CancellationToken ct,
			string packageId,
			UpdaterVersion version,
			Dictionary<string, XmlDocument> projectDocuments,
			bool isDowngradeAllowed
		)
		{
			var operations = new List<UpdateOperation>();

			foreach(var document in projectDocuments)
			{
				var path = document.Key;
				var xmlDocument = document.Value;

				var updates = xmlDocument.UpdateProjectReferenceVersions(packageId, version, path, isDowngradeAllowed);

				if(updates.Any(u => u.IsUpdate))
				{
					await xmlDocument.Save(ct, path);
				}

				operations.AddRange(updates);
			}

			return operations.ToArray();
		}

		private static async Task<UpdateOperation[]> UpdateProjectJson(
			CancellationToken ct,
			string packageName,
			UpdaterVersion latestVersion,
			string[] jsonFiles,
			bool isDowngradeAllowed
		)
		{
			var operations = new List<UpdateOperation>();

			var originalMatch = $@"\""{packageName}\"".*?:.?\""(.*)\""";
			var replaced = $@"""{packageName}"": ""{latestVersion.Version.ToString()}""";

			for(var i = 0; i < jsonFiles.Length; i++)
			{
				var file = jsonFiles[i];
				var fileContent = await FileHelper.ReadFileContent(ct, file);

				var match = Regex.Match(fileContent, originalMatch, RegexOptions.IgnoreCase);
				if(match?.Success ?? false)
				{
					var currentVersion = new NuGetVersion(match.Groups[1].Value);

					var operation = new UpdateOperation(isDowngradeAllowed, packageName, currentVersion, latestVersion, file);

					if(operation.IsUpdate)
					{
						var newContent = Regex.Replace(
							fileContent,
							originalMatch,
							replaced,
							RegexOptions.IgnoreCase
						);

						await FileHelper.SetFileContent(ct, file, newContent);
					}

					operations.Add(operation);
				}
			}

			return operations.ToArray();
		}
	}
}
