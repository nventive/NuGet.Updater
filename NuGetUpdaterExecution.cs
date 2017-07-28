using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using Microsoft.Build.Utilities;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace Nuget.Updater
{
	public class NuGetUpdaterExecution
	{
		private const string MsBuildNamespace = "http://schemas.microsoft.com/developer/msbuild/2003";

		private static TaskLoggingHelper _log;

		public static bool Execute(TaskLoggingHelper log, string solutionRoot, string specialVersion, string excludeTag = "", string PAT = "")
		{
			_log = log;

			var packages = GetPackages(PAT);

			UpdatePackages(solutionRoot, packages, specialVersion, excludeTag);

			return true;
		}

		private static IPackageSearchMetadata[] GetPackages(string PAT)
		{
			var settings = Settings.LoadDefaultSettings(null);
			var repositoryProvider = new SourceRepositoryProvider(settings, Repository.Provider.GetCoreV3());

			var source = new PackageSource("https://nventive.pkgs.visualstudio.com/_packaging/nventive/nuget/v3/index.json", "nventive")
			{
				Credentials = PackageSourceCredential.FromUserInput("nventive", "it@nventive.com", PAT, false)
			};
			var repository = repositoryProvider.CreateRepository(source);

			var searchResource = repository.GetResource<PackageSearchResource>();


			_log?.LogMessage($"Pulling NuGet packages from {source.SourceUri}");
#if DEBUG
			Console.WriteLine($"Pulling NuGet packages from {source.SourceUri}");
#endif

			return searchResource
				.SearchAsync("", new SearchFilter(true, SearchFilterType.IsAbsoluteLatestVersion), 0, 1000, new NullLogger(), CancellationToken.None)
				.Result
				.ToArray();
		}

		private static void UpdatePackages(string solutionRoot, IPackageSearchMetadata[] packages, string specialVersion, string excludeTag)
		{
			var originalNuSpecFiles = Directory.GetFiles(solutionRoot, "*.nuspec", SearchOption.AllDirectories);

			var originalJsonFiles = Directory.GetFiles(solutionRoot, "project.json", SearchOption.AllDirectories);

			var originalProjectFiles = Directory.GetFiles(solutionRoot, "*.csproj", SearchOption.AllDirectories);

			foreach (var package in packages)
			{
				var latestVersion = GetLatestVersion(package, specialVersion, excludeTag);

				if (latestVersion == null)
				{
					continue;
				}

				_log?.LogMessage($"Latest {specialVersion} version for [{package.Title}] is [{latestVersion}]");
#if DEBUG
				Console.WriteLine($"Latest {specialVersion} version for [{package.Title}] is [{latestVersion}]");
#endif

				UpdateNuSpecs(package.Title, latestVersion, originalNuSpecFiles);

				UpdateProjectJson(package.Title, latestVersion, originalJsonFiles);

				UpdateProjects(package.Title, latestVersion, originalProjectFiles);
			}
		}

		private static void UpdateNuSpecs(string packageName, NuGetVersion latestVersion, string[] nuspecFiles)
		{
			foreach (var nuspecFile in nuspecFiles)
			{
				var doc = new XmlDocument()
				{
					PreserveWhitespace = true
				};
				doc.Load(nuspecFile);


				var mgr = new XmlNamespaceManager(doc.NameTable);
				mgr.AddNamespace("x", doc.DocumentElement.NamespaceURI);

				var nodes = doc
					.SelectNodes($"//x:dependency[@id='{packageName}']", mgr)
					.OfType<XmlElement>();

				foreach (var node in nodes)
				{
					var versionNodeValue = node.GetAttribute("version");

					// only nodes with explicit version, skip expansion.
					if (!versionNodeValue.Contains("{"))
					{
						var currentVersion = new NuGetVersion(versionNodeValue);

						if (currentVersion < latestVersion)
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
					doc.Save(nuspecFile);
				}
			}
		}

		private static void UpdateProjectJson(string packageName, NuGetVersion latestVersion, string[] jsonFiles)
		{
			var originalMatch = $@"\""{packageName}\"".*?:.?\""(.*)\""";
			var replaced = $@"""{packageName}"": ""{latestVersion}""";

			for (int i = 0; i < jsonFiles.Length; i++)
			{
				var file = jsonFiles[i];
				var fileContent = File.ReadAllText(file);

				var match = Regex.Match(fileContent, originalMatch, RegexOptions.IgnoreCase);
				if (match?.Success ?? false)
				{
					var currentVersion = new NuGetVersion(match.Groups[1].Value);

					if (currentVersion < latestVersion)
					{
						var newContent = Regex.Replace(
							fileContent,
							originalMatch,
							replaced,
							RegexOptions.IgnoreCase
						);

						File.WriteAllText(file, newContent, Encoding.UTF8);

						LogUpdate(packageName, currentVersion, latestVersion, file);
					}
					else
					{
						LogNoUpdate(packageName, currentVersion, file);
					}
				}
			}
		}

		private static void UpdateProjects(string packageName, NuGetVersion latestVersion, string[] projectFiles)
		{
			for (int i = 0; i < projectFiles.Length; i++)
			{
				var modified = false;

				var doc = new XmlDocument()
				{
					PreserveWhitespace = true
				};
				doc.Load(projectFiles[i]);

				var nsmgr = new XmlNamespaceManager(doc.NameTable);
				nsmgr.AddNamespace("d", MsBuildNamespace);
				modified |= UpdateProjectReferenceVersions(packageName, latestVersion, modified, doc, nsmgr);

				var nsmgr2 = new XmlNamespaceManager(doc.NameTable);
				nsmgr2.AddNamespace("d", "");
				modified |= UpdateProjectReferenceVersions(packageName, latestVersion, modified, doc, nsmgr2);

				if (modified)
				{
					doc.Save(projectFiles[i]);
				}
			}
		}

		private static bool UpdateProjectReferenceVersions(string packageName, NuGetVersion version, bool modified, XmlDocument doc, XmlNamespaceManager nsmgr, string namespaceURI = "")
		{
			foreach (XmlElement packageReference in doc.SelectNodes($"//d:PackageReference[@Include='{packageName}']", nsmgr))
			{
				if (packageReference.HasAttribute("Version", namespaceURI))
				{
					var currentVersion = new NuGetVersion(packageReference.Attributes["Version"].Value);

					if (currentVersion < version)
					{
						packageReference.SetAttribute("Version", namespaceURI, version.ToString());
						modified = true;
						LogUpdate(packageName, currentVersion, version, doc.BaseURI);
					}
					else
					{
						LogNoUpdate(packageName, currentVersion, doc.BaseURI);
					}
				}
				else
				{
					var node = packageReference.SelectSingleNode("d:Version", nsmgr);

					if (node != null)
					{
						var currentVersion = new NuGetVersion(node.InnerText);

						if (currentVersion < version)
						{
							node.InnerText = version.ToString();
							modified = true;
							LogUpdate(packageName, currentVersion, version, doc.BaseURI);
						}
						else
						{
							LogNoUpdate(packageName, currentVersion, doc.BaseURI);
						}
					}
				}
			}

			return modified;
		}

		private static NuGetVersion GetLatestVersion(IPackageSearchMetadata package, string specialVersion, string excludeTag)
		{
			var versions = package.GetVersionsAsync().Result.OrderByDescending(v => v.Version);

			return versions
				.Where(v => IsSpecialVersion(specialVersion, v) && !ContainsTag(excludeTag, v))
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

		private static bool IsSpecialVersion(string specialVersion, VersionInfo version)
		{
			return version.Version?.ReleaseLabels?.Any(label => Regex.IsMatch(label, specialVersion, RegexOptions.IgnoreCase)) ?? false;
		}

		private static void LogUpdate(string packageName, NuGetVersion currentVersion, NuGetVersion newVersion, string file)
		{
			_log?.LogMessage($"Updating [{packageName}] from [{currentVersion}] to [{newVersion}] in [{file}]");
#if DEBUG
			Console.WriteLine($"Updating [{packageName}] from [{currentVersion}] to [{newVersion}] in [{file}]");
#endif
		}

		private static void LogNoUpdate(string packageName, NuGetVersion currentVersion, string file)
		{
			_log?.LogMessage($"Found higher version of [{packageName}] ([{currentVersion}]) in [{file}]");
#if DEBUG
			Console.WriteLine($"Found higher version of [{packageName}] ([{currentVersion}]) in [{file}]");
#endif
		}
	}
}
