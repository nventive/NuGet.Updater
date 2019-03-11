using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Nuget.Updater.Entities;
using Nuget.Updater.Extensions;
using Nuget.Updater.Helpers;
using NuGet.Configuration;
using NuGet.Versioning;

#if UAP
using XmlDocument = Windows.Data.Xml.Dom.XmlDocument;
#else
using XmlDocument = System.Xml.XmlDocument;
#endif

namespace Nuget.Updater
{
	public partial class NuGetUpdater
	{
		private static readonly PackageSource NuGetOrgSource = new PackageSource("https://api.nuget.org/v3/index.json");

		private readonly Parameters _parameters;
		private readonly Logger _log;

		private NuGetUpdater(Parameters parameters, Logger log)
		{
			_parameters = parameters;
			_log = log;
		}

		private async Task<bool> UpdatePackages(CancellationToken ct)
		{
			_log.Clear();

			var packages = await GetPackages(ct);
			var targetFiles = await GetTargetFiles(ct);

			foreach(var package in packages.Where(p => _parameters.ShouldUpdatePackage(p)))
			{
				var latest = await package.GetLatestVersion(ct, _parameters);

				if (latest == null)
				{
					_log.Write($"Skipping [{package.PackageId}]: no {_parameters.TargetVersion} version found.");
					continue;
				}

				_log.Write($"Latest {_parameters.TargetVersion} version for [{package.PackageId}] is [{latest.Version}]");

				foreach(var files in targetFiles)
				{
					var updates = new UpdateOperation[0];

					switch (files.Key)
					{
						case UpdateTarget.Nuspec:
							updates = await UpdateNuSpecs(ct, package.PackageId, latest, files.Value, _parameters.IsDowngradeAllowed);
							break;
						case UpdateTarget.ProjectJson:
							updates = await UpdateProjectJson(ct, package.PackageId, latest, files.Value.Select(p => p.Key).ToArray(), _parameters.IsDowngradeAllowed);
							break;
                        case UpdateTarget.DirectoryProps:
                        case UpdateTarget.DirectoryTargets:
                        case UpdateTarget.PackageReference:
                            updates = await UpdateProjects(ct, package.PackageId, latest, files.Value, _parameters.IsDowngradeAllowed);
							break;
						default:
							break;
					}

					_log.Write(updates);
				}
			}

			_log.WriteSummary(_parameters);

			return true;
		}

		private async Task<NuGetPackage[]> GetPackages(CancellationToken ct)
		{
			//Using search instead of list because the latter forces the v2 api
			var packages = await _parameters.GetFeedPackageSource().SearchPackages(ct, _log.Write);

			if (_parameters.IncludeNuGetOrg)
			{
				//Using search instead of list because the latter forces the v2 api
				packages = packages
					.Concat(await NuGetOrgSource.SearchPackages(ct, _log.Write, searchTerm: "owner:nventive"))
					.GroupBy(p => p.PackageId)
					.Select(g => new NuGetPackage(g.Key, g.ToArray()))
					.ToArray();
			}

			return packages;
		}

		private async Task<Dictionary<UpdateTarget, Dictionary<string, XmlDocument>>> GetTargetFiles(CancellationToken ct)
		{
			var targetFiles = new Dictionary<UpdateTarget, Dictionary<string, XmlDocument>>();

            var updateTarget = new[] {
                UpdateTarget.Nuspec,
                UpdateTarget.PackageReference,
                UpdateTarget.ProjectJson,
                UpdateTarget.DirectoryProps,
                UpdateTarget.DirectoryTargets,
            };

            foreach (var target in updateTarget)
			{
				if (_parameters.HasUpdateTarget(target))
				{
					targetFiles.Add(target, await GetFilesForTarget(ct, target));
				}
			}

			return targetFiles;
		}

		private async Task<Dictionary<string, XmlDocument>> GetFilesForTarget(CancellationToken ct, UpdateTarget target)
		{
			string extensionFilter = null, nameFilter = null;

			switch (target)
			{
				case UpdateTarget.Nuspec:
					extensionFilter = ".nuspec";
					break;

				case UpdateTarget.ProjectJson:
					nameFilter = "project.json";
					break;

                case UpdateTarget.PackageReference:
                    extensionFilter = ".csproj";
                    break;

                case UpdateTarget.DirectoryTargets:
                    extensionFilter = "Directory.Build.targets";
                    break;

                case UpdateTarget.DirectoryProps:
                    extensionFilter = "Directory.Build.props";
                    break;

                default:
					break;
			}

			if (extensionFilter == null && nameFilter == null)
			{
				return new Dictionary<string, XmlDocument>();
			}

			_log.Write($"Retrieving {nameFilter ?? extensionFilter} files");

			var files = await FileHelper.GetFiles(ct, _parameters.SolutionRoot, extensionFilter, nameFilter);

			_log.Write($"Found {files.Length} {nameFilter ?? extensionFilter} file(s)");

			if(target == UpdateTarget.ProjectJson)
			{
				return files.ToDictionary(f => f, f => default(XmlDocument));
			}

			return (await Task.WhenAll(files.Select(f => f.GetDocument(ct))))
				.ToDictionary(p => p.Key, p => p.Value);
		}

		private static async Task<UpdateOperation[]> UpdateNuSpecs(
			CancellationToken ct,
			string packageId,
			FeedNuGetVersion version,
			Dictionary<string, XmlDocument> nuspecDocuments,
			bool isDowngradeAllowed
		)
		{
			var operations = new List<UpdateOperation>();

			foreach (var document in nuspecDocuments)
			{
				var path = document.Key;
				var xmlDocument = document.Value;

				var updates = xmlDocument.UpdateNuSpecVersions(packageId, version, path, isDowngradeAllowed);

				if (updates.Any(u => u.ShouldProceed))
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
			FeedNuGetVersion version,
			Dictionary<string, XmlDocument> projectDocuments,
			bool isDowngradeAllowed
		)
		{
			var operations = new List<UpdateOperation>();

			foreach (var document in projectDocuments)
			{
				var path = document.Key;
				var xmlDocument = document.Value;

				var updates = xmlDocument.UpdateProjectReferenceVersions(packageId, version, path, isDowngradeAllowed);

				if (updates.Any(u => u.ShouldProceed))
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
			FeedNuGetVersion latestVersion,
			string[] jsonFiles,
			bool isDowngradeAllowed
		)
		{
			var operations = new List<UpdateOperation>();

			var originalMatch = $@"\""{packageName}\"".*?:.?\""(.*)\""";
			var replaced = $@"""{packageName}"": ""{latestVersion}""";

			for (int i = 0; i < jsonFiles.Length; i++)
			{
				var file = jsonFiles[i];
				var fileContent = await FileHelper.ReadFileContent(ct, file);

				var match = Regex.Match(fileContent, originalMatch, RegexOptions.IgnoreCase);
				if (match?.Success ?? false)
				{
					var currentVersion = new NuGetVersion(match.Groups[1].Value);

					var operation = new UpdateOperation(isDowngradeAllowed, packageName, currentVersion, latestVersion, file);

					if (operation.ShouldProceed)
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
