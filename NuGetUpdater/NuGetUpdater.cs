using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Nuget.Updater.Entities;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
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

		private static Action<string> _logAction;
		private static bool _allowDowngrade;

		private static readonly List<UpdateOperation> _updateOperations = new List<UpdateOperation>();

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

#if DEBUG
			_logAction = logAction ?? Console.WriteLine;
#else
			_logAction = logAction ?? new Action<string>(_ => { });
#endif
			_allowDowngrade = allowDowngrade;

			var packages = await GetPackages(ct, sourceFeed, PAT, includeNuGetOrg);

			await UpdatePackages(ct, solutionRoot, packages, targetVersion, excludeTag, strict, keepLatestDev, ignorePackages, updatePackages, target);

			LogUpdateSummary(summaryOutputFilePath);

			return true;
		}

		private static void LogUpdateSummary(string outputFilePath = null)
		{
			LogSummary(_logAction);

			if (outputFilePath != null)
			{
				LogUpdateSummaryToFile(outputFilePath);
			}
		}

		private static void LogSummary(Action<string> logAction)
		{
			var completedUpdates = _updateOperations.Where(o => o.ShouldProceed).ToArray();
			var skippedUpdates = _updateOperations.Where(o => !o.ShouldProceed).ToArray();

			if(completedUpdates.Any() || skippedUpdates.Any())
			{
				logAction($"# Package update summary");
			}

			if (completedUpdates.Any())
			{
				var updatedPackages = completedUpdates
					.Select(o => (o.PackageName, o.UpdatedVersion))
					.Distinct()
					.ToArray();

				logAction($"## Updated {updatedPackages.Length} packages:");

				foreach (var p in updatedPackages)
				{
					logAction($"- [{p.PackageName}] to [{p.UpdatedVersion}]");
				}
			}

			if (skippedUpdates.Any())
			{
				var skippedPackages = skippedUpdates
					.Select(o => (o.PackageName, o.PreviousVersion))
					.Distinct()
					.ToArray();

				logAction($"## Skipped {skippedPackages.Length} packages:");

				foreach (var p in skippedPackages)
				{
					logAction($"- [{p.PackageName}] is at version [{p.PreviousVersion}]");
				}
			}
		}

		private static async Task<(string, IPackageSearchMetadata[])[]> GetPackages(CancellationToken ct, string feed, string PAT, bool includNuGetOrg)
		{
			var packages = await GetFeedPackages(ct, feed, PAT);

			if (includNuGetOrg) {
				packages = packages.Concat(await GetNuGetOrgPackages(ct));
			}

			var q = from package in packages
					group package by package.Identity.Id into p
					select (Name: p.Key, Sources: p.ToArray());

			return q.ToArray();
		}

		private static async Task<IEnumerable<IPackageSearchMetadata>> GetNuGetOrgPackages(CancellationToken ct)
		{
			var settings = Settings.LoadDefaultSettings(null);
			var repositoryProvider = new SourceRepositoryProvider(settings, Repository.Provider.GetCoreV3());

			var source = new PackageSource("https://api.nuget.org/v3/index.json");
			var repository = repositoryProvider.CreateRepository(source);

			_logAction($"Pulling NuGet packages from {source.SourceUri}");

			var searchResource = repository.GetResource<PackageSearchResource>();

			var packages = await searchResource.SearchAsync("owner:nventive", new SearchFilter(true, SearchFilterType.IsAbsoluteLatestVersion), 0, 1000, new NullLogger(), ct);

			return packages.ToArray();
		}

		private static async Task<IEnumerable<IPackageSearchMetadata>> GetFeedPackages(CancellationToken ct, string feed, string PAT)
		{
			var settings = Settings.LoadDefaultSettings(null);
			var repositoryProvider = new SourceRepositoryProvider(settings, Repository.Provider.GetCoreV3());

			var source = new PackageSource(feed, "Feed")
			{
				Credentials = PackageSourceCredential.FromUserInput("Feed", "user", PAT, false)
			};
			var repository = repositoryProvider.CreateRepository(source);

			var searchResource = repository.GetResource<PackageSearchResource>();

			_logAction($"Pulling NuGet packages from {source.SourceUri}");

			var packages = await searchResource.SearchAsync("", new SearchFilter(true, SearchFilterType.IsAbsoluteLatestVersion), 0, 1000, new NullLogger(), ct);

			return packages;
		}

		private static async Task UpdatePackages(
			CancellationToken ct,
			string solutionRoot,
			(string title, IPackageSearchMetadata[] sources)[] packages,
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

				foreach(var p in paths)
				{
					var document = await GetDocument(ct, p);
					originalProjectFiles.Add(p, document);
				}
			}

			foreach (var package in packages)
			{
				if (ignoredPackages != null && ignoredPackages.Contains(package.title))
				{
					continue;
				}

				if(packagesToUpdate != null && !packagesToUpdate.Contains(package.title))
				{
					continue;
				}

				var latestVersion = GetLatestVersion(package, targetVersion, excludeTag, strict, keepLatestDev);

				if (latestVersion == null)
				{
					continue;
				}

				_logAction($"Latest {targetVersion} version for [{package.title}] is [{latestVersion}]");

				if ((target & UpdateTarget.Nuspec) == UpdateTarget.Nuspec)
				{
					await UpdateNuSpecs(ct, package.title, latestVersion, originalNuSpecFiles);
				}
				if ((target & UpdateTarget.ProjectJson) == UpdateTarget.ProjectJson)
				{
					await UpdateProjectJson(ct, package.title, latestVersion, originalJsonFiles);
				}
				if ((target & UpdateTarget.PackageReference) == UpdateTarget.PackageReference)
				{
					await UpdateProjects(ct, package.title, latestVersion, originalProjectFiles);
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

			if(extensionFilter == null && nameFilter == null)
			{
				return new string[0];
			}

			_logAction($"Retrieving {nameFilter ?? extensionFilter} files");

			var files = await GetFiles(ct, solutionRootPath, extensionFilter, nameFilter);

			_logAction($"Found {files.Length} {nameFilter ?? extensionFilter} file(s)");

			return files;
		}

		private static async Task UpdateNuSpecs(CancellationToken ct, string packageName, NuGetVersion latestVersion, string[] nuspecFiles)
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

						_logAction(operation.GetLogMessage());
						_updateOperations.Add(operation);
					}
				}

				if (nodes.Any())
				{
					await SaveDocument(ct, doc, nuspecFile);
				}
			}
		}

		private static async Task UpdateProjectJson(CancellationToken ct, string packageName, NuGetVersion latestVersion, string[] jsonFiles)
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

					_logAction(operation.GetLogMessage());
					_updateOperations.Add(operation);
				}
			}
		}

		private static async Task UpdateProjects(CancellationToken ct, string packageName, NuGetVersion latestVersion, Dictionary<string, XmlDocument> projectFiles)
		{
			for (int i = 0; i < projectFiles.Count; i++)
			{
				var modified = false;
				var path = projectFiles.ElementAt(i).Key;
				var document = projectFiles.ElementAt(i).Value;

#if UAP
				modified = UpdateProjectReferenceVersions(packageName, latestVersion, modified, document, path);
#else
				var nsmgr = new XmlNamespaceManager(document.NameTable);
				nsmgr.AddNamespace("d", MsBuildNamespace);
				modified |= UpdateProjectReferenceVersions(packageName, latestVersion, modified, document, path, nsmgr);

				var nsmgr2 = new XmlNamespaceManager(document.NameTable);
				nsmgr2.AddNamespace("d", "");
				modified |= UpdateProjectReferenceVersions(packageName, latestVersion, modified, document, path, nsmgr2);
#endif

				if (modified)
				{
					await SaveDocument(ct, document, path);
				}
			}
		}

		private static NuGetVersion GetLatestVersion((string title, IPackageSearchMetadata[] sources) package, string targetVersion, string excludeTag, bool strict, IEnumerable<string> keepLatestDev = null)
		{
			var versions = package
				.sources
				.SelectMany(s => s.GetVersionsAsync().Result)
				.OrderByDescending(v => v.Version);

			var specialVersion = (keepLatestDev?.Contains(package.title, StringComparer.OrdinalIgnoreCase) ?? false) ? "dev" : targetVersion;

			if(specialVersion == "stable")
			{
				specialVersion = "";
			}

			return versions
				.Where(v => IsMatchingSpecialVersion(specialVersion, v, strict) && !ContainsTag(excludeTag, v))
				.OrderByDescending(v => v.Version)
				.FirstOrDefault()
				?.Version;
		}

		private static bool ContainsTag(string tag, VersionInfo version)
		{
			if (tag?.Equals("") ?? true)
			{
				return false;
			}

			return version?.Version?.ReleaseLabels?.Contains(tag) ?? false;
		}

		private static bool IsMatchingSpecialVersion(string specialVersion, VersionInfo version, bool strict)
		{
			if (string.IsNullOrEmpty(specialVersion))
			{
				return !version.Version?.ReleaseLabels?.Any() ?? true;
			}
			else
			{
				var releaseLabels = version.Version?.ReleaseLabels;
				var isMatchingSpecialVersion = releaseLabels?.Any(label => Regex.IsMatch(label, specialVersion, RegexOptions.IgnoreCase)) ?? false;

				return strict
					? releaseLabels?.Count() == 2 && isMatchingSpecialVersion  // Check strictly for packages with versions "dev.XXXX"
					: isMatchingSpecialVersion; // Allow packages with versions "dev.XXXX.XXXX"
			}
		}
	}
}
