using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using Microsoft.Build.Utilities;
using NuGet.Versioning;

namespace Nuget.Updater
{
	public class NuGetBranchSwitchExecution
	{
		private static TaskLoggingHelper _log;


		private const string MsBuildNamespace = "http://schemas.microsoft.com/developer/msbuild/2003";

		private readonly string[] _packages;
		private readonly string _sourceBranch;
		private readonly string _targetBranch;
		private readonly string _solutionRoot;

		public NuGetBranchSwitchExecution(TaskLoggingHelper log, string solutionRoot, string[] packages, string sourceBranch, string targetBranch)
		{
			_packages = packages;
			_sourceBranch = sourceBranch?.Trim() ?? "";
			_targetBranch = targetBranch?.Trim() ?? "";
			_solutionRoot = solutionRoot;
		}

		public bool Execute()
		{
			var projectFiles = Directory.GetFiles(_solutionRoot, "*.csproj", SearchOption.AllDirectories);
			var nuspecFiles = Directory.GetFiles(_solutionRoot, "*.nuspec", SearchOption.AllDirectories);

			foreach (var package in _packages)
			{
				UpdateProjects(package, projectFiles);
			}

			foreach (var package in _packages)
			{
				UpdateNuSpecs(package, nuspecFiles);
			}

			return true;
		}

		private void UpdateProjects(string packageName, string[] projectFiles)
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
				modified |= UpdateProjectReferenceVersions(packageName, modified, doc, nsmgr);

				var nsmgr2 = new XmlNamespaceManager(doc.NameTable);
				nsmgr2.AddNamespace("d", "");
				modified |= UpdateProjectReferenceVersions(packageName, modified, doc, nsmgr2);

				if (modified)
				{
					doc.Save(projectFiles[i]);
				}
			}
		}

		private bool UpdateProjectReferenceVersions(string packageName, bool modified, XmlDocument doc, XmlNamespaceManager nsmgr, string namespaceURI = "")
		{
			var nodes = doc.SelectNodes($"//d:PackageReference", nsmgr)
				.OfType<XmlElement>()
				.Where(e => Regex.Match(e.GetAttribute("Include"), $"^{packageName}$").Success);

			foreach (var packageReference in nodes)
			{
				(string version, Action<string> updater) GetVersion()
				{
					if (packageReference.HasAttribute("Version", namespaceURI))
					{
						return (
							packageReference.Attributes["Version"].Value,
							v => packageReference.SetAttribute("Version", namespaceURI, v)
						);
					}
					else if (packageReference.SelectSingleNode("d:Version", nsmgr) is XmlNode node)
					{
						return (
							node.InnerText,
							v => node.InnerText = v
						);
					}
					else
					{
						return (null, null);
					}
				}

				var (version, updater) = GetVersion();

				var currentVersion = new NuGetVersion(version);

				var hasUpdateableLabel = GetHasUpdateableLabel(currentVersion);

				if (hasUpdateableLabel ?? false)
				{
					var newVersion = UpdateVersion(currentVersion);

					updater(newVersion.ToFullString());

					LogUpdate(packageReference.GetAttribute("Include"), currentVersion, newVersion, doc.BaseURI);

					modified = true;
				}
			}

			return modified;
		}

		private bool? GetHasUpdateableLabel(NuGetVersion currentVersion)
		{
			if (currentVersion.ReleaseLabels?.Any() ?? false)
			{
				return currentVersion.ReleaseLabels?.Any(l => l.Equals(_sourceBranch, StringComparison.OrdinalIgnoreCase));
			}
			else
			{
				return string.IsNullOrEmpty(_sourceBranch);
			}
		}

		private NuGetVersion UpdateVersion(NuGetVersion currentVersion)
		{
			var updatedLabels = string.IsNullOrEmpty(_sourceBranch)
				? new[] { _targetBranch }
				: currentVersion.ReleaseLabels.Select(l => l.Replace(_sourceBranch, _targetBranch)).ToArray();

			var newVersion = new NuGetVersion(
				major: currentVersion.Major,
				minor: currentVersion.Minor,
				patch: currentVersion.Patch,
				revision: currentVersion.Revision,
				releaseLabels: updatedLabels,
				metadata: currentVersion.Metadata
			);

			return newVersion;
		}

		private void UpdateNuSpecs(string packageName, string[] nuspecFiles)
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
					.SelectNodes($"//x:dependency", mgr)
					.OfType<XmlElement>()
					.Where(e => Regex.Match(e.GetAttribute("id"), $"^{packageName}$").Success);

				foreach (var node in nodes)
				{
					var versionNodeValue = node.GetAttribute("version");

					// only nodes with explicit version, skip expansion.
					if (!versionNodeValue.Contains("{"))
					{
						var currentVersion = new NuGetVersion(versionNodeValue);

						var hasUpdateableLabel = GetHasUpdateableLabel(currentVersion);

						if (hasUpdateableLabel ?? false)
						{
							var newVersion = UpdateVersion(currentVersion);

							node.SetAttribute("version", newVersion.ToFullString());

							LogUpdate(node.GetAttribute("id"), currentVersion, newVersion, doc.BaseURI);
						}
					}
				}

				if (nodes.Any())
				{
					doc.Save(nuspecFile);
				}
			}
		}

		private static void LogUpdate(string packageName, NuGetVersion currentVersion, NuGetVersion newVersion, string file)
		{
			_log?.LogMessage($"Updating [{packageName}] from [{currentVersion}] to [{newVersion}] in [{file}]");
#if DEBUG
			Console.WriteLine($"Updating [{packageName}] from [{currentVersion}] to [{newVersion}] in [{file}]");
#endif
		}
	}
}