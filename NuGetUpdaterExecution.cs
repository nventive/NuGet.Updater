using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using Microsoft.Build.Utilities;
using NuGet;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace Nuget.Updater
{
	public class NuGetUpdaterExecution
	{
		private const string MsBuildNamespace = "http://schemas.microsoft.com/developer/msbuild/2003";

		public static bool Execute(TaskLoggingHelper log, string solutionRoot, string specialVersion, string excludeTag = "", string PAT = "")
		{
			if (excludeTag == null || excludeTag.Trim() == "")
			{
				excludeTag = "clear";
			}

			var packages = GetPackages(PAT);

			UpdateProjectJson(log, solutionRoot, packages, specialVersion, excludeTag);
			UpdateProject(log, solutionRoot, packages, specialVersion, excludeTag);
			UpdateNuSpecs(log, solutionRoot, packages, specialVersion, excludeTag);

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

			return searchResource
				.SearchAsync("", new SearchFilter(true, SearchFilterType.IsAbsoluteLatestVersion), 0, 1000, new NullLogger(), CancellationToken.None)
				.Result
				.ToArray();
		}

		private static void UpdateNuSpecs(TaskLoggingHelper log, string solutionRoot, IPackageSearchMetadata[] packages, string specialVersion, string excludeTag)
		{
			var originalNuSpecFiles = Directory.GetFiles(solutionRoot, "*.nuspec", SearchOption.AllDirectories);

			log?.LogMessage($"Updating nuspec files...");

			foreach (var package in packages)
			{
				var latestVersion = GetLatestVersion(package, specialVersion, excludeTag);

				if(latestVersion == null)
				{
					continue;
				}

				log?.LogMessage($"Latest version for [{package.Title}] is [{latestVersion.Version}]");

#if DEBUG
				Console.WriteLine($"Latest version for [{package.Title}] is [{latestVersion.Version}]");
#endif

				if (latestVersion != null)
				{
					foreach (var nuspecFile in originalNuSpecFiles)
					{
						var doc = new XmlDocument()
						{
							PreserveWhitespace = true
						};
						doc.Load(nuspecFile);


						XmlNamespaceManager mgr = new XmlNamespaceManager(doc.NameTable);
						mgr.AddNamespace("x", doc.DocumentElement.NamespaceURI);

						var nodes = doc.SelectNodes($"//x:dependency[@id='{package.Title}']", mgr).OfType<XmlElement>();

						if (nodes.Any())
						{
							foreach (var node in nodes)
							{
								if (!node.GetAttribute("version").Contains("{"))
								{
									// only nodes with explicit version, skip expansion.
									node.SetAttribute("version", latestVersion.Version.ToString());
								}
							}

							doc.Save(nuspecFile);
						}
					}
					
				}
			}
		}

		private static void UpdateProjectJson(TaskLoggingHelper log, string solutionRoot, IPackageSearchMetadata[] packages, string specialVersion, string excludeTag)
		{
			var originalFiles = Directory.GetFiles(solutionRoot, "project.json", SearchOption.AllDirectories);
			var originalContent = originalFiles.Select(File.ReadAllText).ToArray();
			var filesContent = originalContent.ToArray();

			log?.LogMessage($"Updating project.json files...");

			foreach (var package in packages)
			{
				var latestVersion = GetLatestVersion(package, specialVersion, excludeTag);

				if (latestVersion == null)
				{
					continue;
				}

				log?.LogMessage($"Latest version for [{package.Title}] is [{latestVersion.Version}]");

				if (latestVersion != null)
				{
					for (int i = 0; i < filesContent.Length; i++)
					{
						var originalMatch = $@"\""{package.Title}\"".*?\:.*?\"".*?\""";
						var replaced = $@"""{package.Title}"": ""{latestVersion.Version}""";

						var newContent = Regex.Replace(
							filesContent[i],
							originalMatch,
							replaced
							, RegexOptions.IgnoreCase
						);

						filesContent[i] = newContent;
					}
				}
			}

			for (int i = 0; i < originalFiles.Length; i++)
			{
				if (!filesContent[i].Equals(originalContent[i]))
				{
					log?.LogMessage($"Updating [{originalFiles[i]}] with [{specialVersion}] releases");

					File.WriteAllText(originalFiles[i], filesContent[i], Encoding.UTF8);
				}
			}
		}

		private static void UpdateProject(TaskLoggingHelper log, string solutionRoot, IPackageSearchMetadata[] packages, string specialVersion, string excludeTag)
		{
			var originalFiles = Directory.GetFiles(solutionRoot, "*.csproj", SearchOption.AllDirectories);

			log?.LogMessage($"Updating PackageReference nodes...");

			foreach (var package in packages)
			{
				var latestVersion = GetLatestVersion(package, specialVersion, excludeTag);

				if (latestVersion == null)
				{
					continue;
				}

				log?.LogMessage($"Latest version for [{package.Title}] is [{latestVersion.Version}]");

				if (latestVersion != null)
				{
					for (int i = 0; i < originalFiles.Length; i++)
					{
						var modified = false;

						var doc = new XmlDocument()
						{
							PreserveWhitespace = true
						};
						doc.Load(originalFiles[i]);

						var nsmgr = new XmlNamespaceManager(doc.NameTable);
						nsmgr.AddNamespace("d", MsBuildNamespace);
						modified |= UpdateProjectReferenceVersions(package.Title, latestVersion.Version, modified, doc, nsmgr, MsBuildNamespace);

						var nsmgr2 = new XmlNamespaceManager(doc.NameTable);
						nsmgr2.AddNamespace("d", "");
						modified |= UpdateProjectReferenceVersions(package.Title, latestVersion.Version, modified, doc, nsmgr2);

						if (modified)
						{
							log?.LogMessage($"Updating [{originalFiles[i]}] with [{specialVersion}] releases");
							doc.Save(originalFiles[i]);
						}
					}
				}
			}
		}

		private static bool UpdateProjectReferenceVersions(string packageName, NuGetVersion version, bool modified, XmlDocument doc, XmlNamespaceManager nsmgr, string namespaceURI = "")
		{
			foreach (XmlElement packageReference in doc.SelectNodes($"//d:PackageReference[@Include='{packageName}']", nsmgr))
			{
				if (packageReference.HasAttribute("Version", namespaceURI))
				{
					packageReference.SetAttribute("Version", namespaceURI, version.ToString());
					modified = true;
				}
				else
				{
					var node = packageReference.SelectSingleNode("d:Version", nsmgr);

					if (node != null)
					{
						node.InnerText = version.ToString();
						modified = true;
					}
				}
			}

			return modified;
		}

		private static VersionInfo GetLatestVersion(IPackageSearchMetadata package, string specialVersion, string excludeTag)
		{
			var versions = package.GetVersionsAsync().Result;

			return versions
				.Where(v => IsSpecialVersion(specialVersion, v) && !ContainsTag(excludeTag, v))
				.OrderByDescending(v => v.Version.Version)
				.FirstOrDefault();
		}

		private static bool ContainsTag(string tag, VersionInfo version)
		{
			if (tag.Equals(""))
			{
				return true;
			}

			return version.Version?.ReleaseLabels?.Contains(tag) ?? false;
		}

		private static bool IsSpecialVersion(string specialVersion, VersionInfo version)
		{
			return version.Version?.ReleaseLabels?.Any(label => Regex.IsMatch(label, specialVersion, RegexOptions.IgnoreCase)) ?? false;
		}
	}
}
