using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Nuget.Updater.Entities;
using Nuget.Updater.Extensions;
using NuGet.Configuration;
using NuGet.Versioning;

#if UAP
using XmlDocument = Windows.Data.Xml.Dom.XmlDocument;
using XmlElement = System.Xml.XmlElement;
#else
using XmlDocument = System.Xml.XmlDocument;
using XmlElement = System.Xml.XmlElement;
using XmlNamespaceManager = System.Xml.XmlNamespaceManager;
#endif

namespace Nuget.Updater
{
	public partial class NuGetUpdater
	{
		private const string MsBuildNamespace = "http://schemas.microsoft.com/developer/msbuild/2003";

		private static bool _allowDowngrade;

		public static bool Update(
			string solutionRoot,
			string sourceFeed,
			string targetVersion,
			string excludeTag = "",
			string PAT = "",
			bool includeNuGetOrg = true,
			bool allowDowngrade = false,
			bool strict = true,
			IEnumerable<string> keepLatestDev = null,
			IEnumerable<string> ignorePackages = null,
			IEnumerable<string> updatePackages = null,
			UpdateTarget target = UpdateTarget.All,
			Action<string> logAction = null,
			string summaryOutputFilePath = null
		)
		{
			return UpdateAsync(
				CancellationToken.None,
				solutionRoot,
				sourceFeed,
				targetVersion,
				excludeTag,
				PAT,
				includeNuGetOrg,
				allowDowngrade,
				strict,
				keepLatestDev,
				ignorePackages,
				updatePackages,
				target,
				logAction,
				summaryOutputFilePath
			).Result;
		}

		public static async Task<bool> UpdateAsync(
			CancellationToken ct,
			string solutionRoot,
			string sourceFeed,
			string targetVersion,
			string excludeTag = "",
			string PAT = "",
			bool includeNuGetOrg = true,
			bool allowDowngrade = false,
			bool strict = true,
			IEnumerable<string> keepLatestDev = null,
			IEnumerable<string> ignorePackages = null,
			IEnumerable<string> updatePackages = null,
			UpdateTarget target = UpdateTarget.All,
			Action<string> logAction = null,
			string summaryOutputFilePath = null
		)
		{
			_updateOperations.Clear();

			_logAction = logAction
#if DEBUG
				?? Console.WriteLine;
#else
				?? new Action<string>(_ => { });
#endif
			_allowDowngrade = allowDowngrade;

			var packages = await GetPackages(ct, sourceFeed, PAT, includeNuGetOrg);

			await UpdatePackages(ct, solutionRoot, packages, targetVersion, excludeTag, strict, keepLatestDev, ignorePackages, updatePackages, target);

			LogUpdateSummary(summaryOutputFilePath);

			return true;
		}

		private static async Task<NuGetPackage[]> GetPackages(CancellationToken ct, string feed, string PAT, bool includNuGetOrg)
		{
			var privateSource = new PackageSource(feed, "Feed")
			{
				Credentials = PackageSourceCredential.FromUserInput("Feed", "user", PAT, false)
			};

			//Using search instead of list because the latter forces the v2 api
			var packages = await privateSource.SearchPackages(ct, Log);

			if (includNuGetOrg)
			{
				var publicSource = new PackageSource("https://api.nuget.org/v3/index.json");

				//Using search instead of list because the latter forces the v2 api
				packages = packages
					.Concat(await publicSource.SearchPackages(ct, Log, searchTerm: "owner:nventive"))
					.GroupBy(p => p.PackageId)
					.Select(g => new NuGetPackage(g.Key, g.ToArray()))
					.ToArray();
			}

			return packages;
		}

		private static async Task UpdatePackages(
			CancellationToken ct,
			string solutionRoot,
			NuGetPackage[] packages,
			string targetVersion,
			string excludeTag,
			bool strict,
			IEnumerable<string> keepLatestDev = null,
			IEnumerable<string> ignoredPackages = null,
			IEnumerable<string> packagesToUpdate = null,
			UpdateTarget target = UpdateTarget.All
		)
		{
			var originalNuSpecFiles = new string[0];
			var originalJsonFiles = new string[0];
			var originalProjectFiles = new Dictionary<string, XmlDocument>();

			if ((target & UpdateTarget.Nuspec) == UpdateTarget.Nuspec)
			{
				originalNuSpecFiles = await GetTargetFiles(ct, solutionRoot, UpdateTarget.Nuspec);
			}
			if ((target & UpdateTarget.ProjectJson) == UpdateTarget.ProjectJson)
			{
				originalJsonFiles = await GetTargetFiles(ct, solutionRoot, UpdateTarget.ProjectJson);
			}
			if ((target & UpdateTarget.PackageReference) == UpdateTarget.PackageReference)
			{
				var paths = await GetTargetFiles(ct, solutionRoot, UpdateTarget.PackageReference);

				foreach (var p in paths)
				{
					var document = await GetDocument(ct, p);
					originalProjectFiles.Add(p, document);
				}
			}

			foreach (var package in packages)
			{
				var packageId = package.PackageId;

				if (ignoredPackages != null && ignoredPackages.Contains(packageId))
				{
					continue;
				}

				if ((packagesToUpdate?.Any() ?? false) && !packagesToUpdate.Contains(packageId))
				{
					continue;
				}

				var latest = await package.GetLatestVersion(ct, targetVersion, excludeTag, strict, keepLatestDev);

				if (latest == null)
				{
					continue;
				}

				Log($"Latest {targetVersion} version for [{packageId}] is [{latest.Version}]");

				if ((target & UpdateTarget.Nuspec) == UpdateTarget.Nuspec)
				{
					await UpdateNuSpecs(ct, packageId, latest, originalNuSpecFiles);
				}
				if ((target & UpdateTarget.ProjectJson) == UpdateTarget.ProjectJson)
				{
					await UpdateProjectJson(ct, packageId, latest, originalJsonFiles);
				}
				if ((target & UpdateTarget.PackageReference) == UpdateTarget.PackageReference)
				{
					await UpdateProjects(ct, packageId, latest, originalProjectFiles);
				}
			}
		}

		private static async Task<string[]> GetTargetFiles(CancellationToken ct, string solutionRootPath, UpdateTarget target)
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
				case UpdateTarget.All:
				default:
					break;
			}

			if (extensionFilter == null && nameFilter == null)
			{
				return new string[0];
			}

			Log($"Retrieving {nameFilter ?? extensionFilter} files");

			var files = await GetFiles(ct, solutionRootPath, extensionFilter, nameFilter);

			Log($"Found {files.Length} {nameFilter ?? extensionFilter} file(s)");

			return files;
		}

		private static async Task UpdateNuSpecs(CancellationToken ct, string packageName, FeedNuGetVersion latestVersion, string[] nuspecFiles)
		{
			foreach (var nuspecFile in nuspecFiles)
			{
				var doc = await GetDocument(ct, nuspecFile);

#if UAP
				var nodes = doc
					.SelectNodes($"//x:dependency[@id='{packageName}']")
					.OfType<XmlElement>();
#else
				var mgr = new XmlNamespaceManager(doc.NameTable);
				mgr.AddNamespace("x", doc.DocumentElement.NamespaceURI);

				var nodes = doc
					.SelectNodes($"//x:dependency[@id='{packageName}']", mgr)
					.OfType<XmlElement>();
#endif

				foreach (var node in nodes)
				{
					var versionNodeValue = node.GetAttribute("version");

					// only nodes with explicit version, skip expansion.
					if (!versionNodeValue.Contains("{"))
					{
						var currentVersion = new NuGetVersion(versionNodeValue);

						var operation = new UpdateOperation(_allowDowngrade, packageName, currentVersion, latestVersion, nuspecFile);

						if (operation.ShouldProceed)
						{
							node.SetAttribute("version", latestVersion.ToString());
						}

						Log(operation);
					}
				}

				if (nodes.Any())
				{
					await SaveDocument(ct, doc, nuspecFile);
				}
			}
		}

		private static async Task UpdateProjectJson(CancellationToken ct, string packageName, FeedNuGetVersion latestVersion, string[] jsonFiles)
		{
			var originalMatch = $@"\""{packageName}\"".*?:.?\""(.*)\""";
			var replaced = $@"""{packageName}"": ""{latestVersion}""";

			for (int i = 0; i < jsonFiles.Length; i++)
			{
				var file = jsonFiles[i];
				var fileContent = await ReadFileContent(ct, file);

				var match = Regex.Match(fileContent, originalMatch, RegexOptions.IgnoreCase);
				if (match?.Success ?? false)
				{
					var currentVersion = new NuGetVersion(match.Groups[1].Value);

					var operation = new UpdateOperation(_allowDowngrade, packageName, currentVersion, latestVersion, file);

					if (operation.ShouldProceed)
					{
						var newContent = Regex.Replace(
							fileContent,
							originalMatch,
							replaced,
							RegexOptions.IgnoreCase
						);

						await SetFileContent(ct, file, newContent);
					}

					Log(operation);
				}
			}
		}

		private static async Task UpdateProjects(CancellationToken ct, string packageName, FeedNuGetVersion latestVersion, Dictionary<string, XmlDocument> projectFiles)
		{
			for (int i = 0; i < projectFiles.Count; i++)
			{
				var path = projectFiles.ElementAt(i).Key;
				var document = projectFiles.ElementAt(i).Value;

				if (UpdateProjectReferenceVersions(packageName, latestVersion, document, path))
				{
					await SaveDocument(ct, document, path);
				}
			}
		}
	}
}
