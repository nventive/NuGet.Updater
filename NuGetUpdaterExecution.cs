using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using Microsoft.Build.Utilities;
using NuGet;
using Uno.Extensions;

namespace Nuget.Updater
{
	public class NuGetUpdaterExecution
	{
		private const string MsBuildNamespace = "http://schemas.microsoft.com/developer/msbuild/2003";

		public static bool Execute(TaskLoggingHelper log, string solutionRoot, string[] packageSources, string[] packages, string specialVersion, string excludeTag = "", string PAT = "")
		{
			if (excludeTag.IsNullOrWhiteSpace())
			{
				excludeTag = "clear";
			}

			NuGet.HttpClient.DefaultCredentialProvider = new LocalNugetProvider("jerome.laban@nventive.com", PAT);

			UpdateProjectJson(log, solutionRoot, packageSources, packages, specialVersion, excludeTag);
			UpdateProject(log, solutionRoot, packageSources, packages, specialVersion, excludeTag);
			UpdateNuSpecs(log, solutionRoot, packageSources, packages, specialVersion, excludeTag);

			return true;
		}

		private static void UpdateNuSpecs(TaskLoggingHelper log, string solutionRoot, string[] packageSources, string[] packages, string specialVersion, string excludeTag)
		{
			var originalNuSpecFiles = Directory.GetFiles(solutionRoot, "*.nuspec", SearchOption.AllDirectories);

			log?.LogMessage($"Updating nuspec files...");

			foreach (var package in packages)
			{
				var latestVersion = GetPackagesVersion(packageSources, package, specialVersion, excludeTag).FirstOrDefault();

				log?.LogMessage($"Latest version for [{package}] is [{latestVersion}]");

#if DEBUG
				Console.WriteLine($"Latest version for [{package}] is [{latestVersion}]");
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

						var nodes = doc.SelectNodes($"//x:dependency[@id='{package}']", mgr).OfType<XmlElement>();

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

		private static void UpdateProjectJson(TaskLoggingHelper log, string solutionRoot, string[] packageSources, string[] packages, string specialVersion, string excludeTag)
		{
			var originalFiles = Directory.GetFiles(solutionRoot, "project.json", SearchOption.AllDirectories);
			var originalContent = originalFiles.Select(File.ReadAllText).ToArray();
			var filesContent = originalContent.ToArray();

			log?.LogMessage($"Updating project.json files...");

			foreach (var package in packages)
			{
				var latestVersion = GetPackagesVersion(packageSources, package, specialVersion, excludeTag).FirstOrDefault();

				log?.LogMessage($"Latest version for [{package}] is [{latestVersion}]");

				if (latestVersion != null)
				{
					for (int i = 0; i < filesContent.Length; i++)
					{
						var originalMatch = $@"\""{latestVersion.Id}\"".*?\:.*?\"".*?\""";
						var replaced = $@"""{latestVersion.Id}"": ""{latestVersion.Version}""";

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
		private static void UpdateProject(TaskLoggingHelper log, string solutionRoot, string[] packageSources, string[] packages, string specialVersion, string excludeTag)
		{
			var originalFiles = Directory.GetFiles(solutionRoot, "*.csproj", SearchOption.AllDirectories);

			log?.LogMessage($"Updating PackageReference nodes...");

			foreach (var package in packages)
			{
				var latestVersion = GetPackagesVersion(packageSources, package, specialVersion, excludeTag).FirstOrDefault();

				log?.LogMessage($"Latest version for [{package}] is [{latestVersion}]");

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
						modified |= UpdateProjectReferenceVersions(latestVersion, modified, doc, nsmgr);

						var nsmgr2 = new XmlNamespaceManager(doc.NameTable);
						nsmgr2.AddNamespace("d", "");
						modified |= UpdateProjectReferenceVersions(latestVersion, modified, doc, nsmgr2);

						if (modified)
						{
							doc.Save(originalFiles[i]);
						}
					}
				}
			}
		}

		private static bool UpdateProjectReferenceVersions(IPackage latestVersion, bool modified, XmlDocument doc, XmlNamespaceManager nsmgr)
		{
			foreach (XmlElement packageReference in doc.SelectNodes($"//d:PackageReference[@Include='{latestVersion.Id}']", nsmgr))
			{
				if (packageReference.HasAttribute("Version", MsBuildNamespace))
				{
					packageReference.SetAttribute("Version", MsBuildNamespace, latestVersion.Version.ToString());
					modified = true;
				}
				else
				{
					var node = packageReference.SelectSingleNode("d:Version", nsmgr);

					if (node != null)
					{
						node.InnerText = latestVersion.Version.ToString();
						modified = true;
					}
				}
			}

			return modified;
		}

		private static IEnumerable<IPackage> GetPackagesVersion(string[] packageSources, string packageId, string specialVersion, string excludeTag)
		{
			foreach (var source in packageSources)
			{
				var repo = PackageRepositoryFactory.Default.CreateRepository("https://nventive.pkgs.visualstudio.com/_packaging/nventive/nuget/v2");

				var packages = repo.FindPackagesById(packageId);

				packages = packages
					.Where(item => IsSpecialVersion(specialVersion, item) && !ContainsTag(excludeTag, item))
					.OrderByDescending(item => item.Version);

				foreach (IPackage p in packages)
				{
					yield return p;
				}
			}
		}

		private static bool ContainsTag(string tag, IPackage item)
		{
			if (tag.Equals(""))
			{
				return true;
			}

			return item.Version.SpecialVersion?.Contains(tag) ?? false;
		}

		private static bool IsSpecialVersion(string specialVersion, IPackage item)
		{
			if (item.Version.SpecialVersion.HasValue())
			{
				return Regex.IsMatch(item.Version.SpecialVersion, specialVersion, RegexOptions.IgnoreCase);
			}

			return false;
		}
	}
}
