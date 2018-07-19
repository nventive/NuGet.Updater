using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
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

		public static bool Update(
			string solutionRoot,
			string targetVersion,
			string excludeTag = "",
			string PAT = "",
			bool allowDowngrade = false,
			bool strict = true,
			IEnumerable<string> keepLatestDev = null,
			IEnumerable<string> ignorePackages = null,
			Action<string> logAction = null
		)
		{
#if DEBUG
			_logAction = Console.WriteLine;
#else
			_logAction = logAction ?? new Action<string>(_ => { });
#endif
			_allowDowngrade = allowDowngrade;

			var packages = GetPackages(PAT);

			UpdatePackages(solutionRoot, packages, targetVersion, excludeTag, strict, keepLatestDev, ignorePackages);

			return true;
		}

		private static (string, IPackageSearchMetadata[])[] GetPackages(string PAT)
		{
			var q = from package in GetVSTSPackages(PAT)
						   .Concat(GetNuGetOrgPackages())
					group package by package.Identity.Id into p
					select (
						Name: p.Key,
						Sources: p.ToArray()
					);

			return q.ToArray();
		}

		private static IEnumerable<IPackageSearchMetadata> GetNuGetOrgPackages()
		{
			var settings = Settings.LoadDefaultSettings(null);
			var repositoryProvider = new SourceRepositoryProvider(settings, Repository.Provider.GetCoreV3());

			var source = new PackageSource("https://api.nuget.org/v3/index.json");
			var repository = repositoryProvider.CreateRepository(source);

			_logAction($"Pulling NuGet packages from {source.SourceUri}");

			var searchResource = repository.GetResource<PackageSearchResource>();

			return searchResource
				.SearchAsync("owner:nventive", new SearchFilter(true, SearchFilterType.IsAbsoluteLatestVersion), 0, 1000, new NullLogger(), CancellationToken.None)
				.Result
				.ToArray();
		}

		private static IPackageSearchMetadata[] GetVSTSPackages(string PAT)
		{
			var settings = Settings.LoadDefaultSettings(null);
			var repositoryProvider = new SourceRepositoryProvider(settings, Repository.Provider.GetCoreV3());

			var source = new PackageSource("https://nventive.pkgs.visualstudio.com/_packaging/nventive/nuget/v3/index.json", "nventive")
			{
				Credentials = PackageSourceCredential.FromUserInput("nventive", "it@nventive.com", PAT, false)
			};
			var repository = repositoryProvider.CreateRepository(source);

			var searchResource = repository.GetResource<PackageSearchResource>();

			_logAction($"Pulling NuGet packages from {source.SourceUri}");

			return searchResource
				.SearchAsync("", new SearchFilter(true, SearchFilterType.IsAbsoluteLatestVersion), 0, 1000, new NullLogger(), CancellationToken.None)
				.Result
				.ToArray();
		}

		private static async void UpdatePackages(
			string solutionRoot,
			(string title, IPackageSearchMetadata[] sources)[] packages,
			string targetVersion,
			string excludeTag,
			bool strict,
			IEnumerable<string> keepLatestDev = null,
			IEnumerable<string> ignoredPackages = null)
		{
			var ct = CancellationToken.None;

			var originalNuSpecFiles = await GetFiles(ct, solutionRoot, extensionFilter: ".nuspec");

			var originalJsonFiles = await GetFiles(ct, solutionRoot, nameFilter: "project.json");

			var originalProjectFiles = await GetFiles(ct, solutionRoot, extensionFilter: ".csproj");

			foreach (var package in packages)
			{
				if (ignoredPackages != null && ignoredPackages.Contains(package.title))
				{
					continue;
				}

				var latestVersion = GetLatestVersion(package, targetVersion, excludeTag, strict, keepLatestDev);

				if (latestVersion == null)
				{
					continue;
				}

				_logAction($"Latest {targetVersion} version for [{package.title}] is [{latestVersion}]");

				await UpdateNuSpecs(ct, package.title, latestVersion, originalNuSpecFiles);

				await UpdateProjectJson(ct, package.title, latestVersion, originalJsonFiles);

				await UpdateProjects(ct, package.title, latestVersion, originalProjectFiles);
			}
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

						if (currentVersion == latestVersion)
						{
							LogNoUpdate(packageName, currentVersion, nuspecFile, isLatest: true);
						}
						else if (currentVersion < latestVersion || _allowDowngrade)
						{
							node.SetAttribute("version", latestVersion.ToString());
							LogUpdate(packageName, currentVersion, latestVersion, nuspecFile);
						}
						else
						{
							LogNoUpdate(packageName, currentVersion, nuspecFile);
						}
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

					if (currentVersion == latestVersion)
					{
						LogNoUpdate(packageName, currentVersion, file, isLatest: true);
					}
					else if (currentVersion < latestVersion || _allowDowngrade)
					{
						var newContent = Regex.Replace(
							fileContent,
							originalMatch,
							replaced,
							RegexOptions.IgnoreCase
						);

						await SetFileContent(ct, file, newContent);

						LogUpdate(packageName, currentVersion, latestVersion, file);
					}
					else
					{
						LogNoUpdate(packageName, currentVersion, file);
					}
				}
			}
		}

		private static async Task UpdateProjects(CancellationToken ct, string packageName, NuGetVersion latestVersion, string[] projectFiles)
		{
			for (int i = 0; i < projectFiles.Length; i++)
			{
				var modified = false;
				var path = projectFiles[i];
				var doc = await GetDocument(ct, path);

#if UAP
				modified = UpdateProjectReferenceVersions(packageName, latestVersion, modified, doc, path);
#else
				var nsmgr = new XmlNamespaceManager(doc.NameTable);
				nsmgr.AddNamespace("d", MsBuildNamespace);
				modified |= UpdateProjectReferenceVersions(packageName, latestVersion, modified, doc, path, nsmgr);

				var nsmgr2 = new XmlNamespaceManager(doc.NameTable);
				nsmgr2.AddNamespace("d", "");
				modified |= UpdateProjectReferenceVersions(packageName, latestVersion, modified, doc, path, nsmgr2);
#endif

				if (modified)
				{
					await SaveDocument(ct, doc, projectFiles[i]);
				}
			}
		}

#if UAP
		private static bool UpdateProjectReferenceVersions(string packageName, NuGetVersion version, bool modified, XmlDocument doc, string documentPath)
#else
		private static bool UpdateProjectReferenceVersions(string packageName, NuGetVersion version, bool modified, XmlDocument doc, string documentPath, XmlNamespaceManager namespaceManager)
#endif
		{
			try
			{
				foreach (XmlElement packageReference in doc.SelectNodes($"//d:PackageReference[@Include='{packageName}']"))
				{
					if (packageReference.HasAttribute("Version"))
					{
						var currentVersion = new NuGetVersion(packageReference.Attributes["Version"].Value);

						if (currentVersion == version)
						{
							LogNoUpdate(packageName, currentVersion, documentPath, isLatest: true);
						}
						else if (currentVersion < version || _allowDowngrade)
						{
							packageReference.SetAttribute("Version", version.ToString());
							modified = true;
							LogUpdate(packageName, currentVersion, version, documentPath);
						}
						else
						{
							LogNoUpdate(packageName, currentVersion, documentPath);
						}
					}
					else
					{
#if UAP
						var node = packageReference.SelectSingleNode("d:Version");
#else
						var node = packageReference.SelectSingleNode("d:Version", namespaceManager);
#endif

						if (node != null)
						{
							var currentVersion = new NuGetVersion(node.InnerText);

							if (currentVersion == version)
							{
								LogNoUpdate(packageName, currentVersion, documentPath, isLatest: true);
							}
							else if (currentVersion < version || _allowDowngrade)
							{
								node.InnerText = version.ToString();
								modified = true;
								LogUpdate(packageName, currentVersion, version, documentPath);
							}
							else
							{
								LogNoUpdate(packageName, currentVersion, documentPath);
							}
						}
					}
				}

				return modified;
			}
			catch(Exception ex)
			{
				//Probably means that the package hasn't been found
			}

			return false;
		}

		private static NuGetVersion GetLatestVersion((string title, IPackageSearchMetadata[] sources) package, string targetVersion, string excludeTag, bool strict, IEnumerable<string> keepLatestDev = null)
		{
			var versions = package
				.sources
				.SelectMany(s => s.GetVersionsAsync().Result)
				.OrderByDescending(v => v.Version);

			var specialVersion = (keepLatestDev?.Contains(package.title, StringComparer.OrdinalIgnoreCase) ?? false) ? "dev" : targetVersion;

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

		private static void LogUpdate(string packageName, NuGetVersion currentVersion, NuGetVersion newVersion, string file)
		{
			_logAction($"Updating [{packageName}] from [{currentVersion}] to [{newVersion}] in [{file}]");
		}

		private static void LogNoUpdate(string packageName, NuGetVersion currentVersion, string file, bool isLatest = false)
		{
			_logAction($"Found {(isLatest ? "latest" : "higher")} version of [{packageName}] ([{currentVersion}]) in [{file}]");
		}
	}
}
