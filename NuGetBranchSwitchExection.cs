using System;
using System.IO;
using System.Linq;
using System.Xml;
using Microsoft.Build.Utilities;
using NuGet.Versioning;

namespace Nuget.Updater
{
	public class NuGetBranchSwitchExection
	{
		private static TaskLoggingHelper _log;


		private const string MsBuildNamespace = "http://schemas.microsoft.com/developer/msbuild/2003";

		private readonly string[] _packages;
		private readonly string _sourceBranch;
		private readonly string _targetBranch;
		private readonly string _solutionRoot;

		public NuGetBranchSwitchExection(TaskLoggingHelper log, string solutionRoot, string[] packages, string sourceBranch, string targetBranch)
		{
			_packages = packages;
			_sourceBranch = sourceBranch;
			_targetBranch = targetBranch;
			_solutionRoot = solutionRoot;
		}

		public bool Execute()
		{
			var projectFiles = Directory.GetFiles(_solutionRoot, "*.csproj", SearchOption.AllDirectories);

			foreach(var package in _packages)
			{
				UpdateProjects(package, projectFiles);
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
			foreach (XmlElement packageReference in doc.SelectNodes($"//d:PackageReference[@Include='{packageName}']", nsmgr))
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
					else if(packageReference.SelectSingleNode("d:Version", nsmgr) is XmlNode node)
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

				var hasUpdateableLabel = currentVersion.ReleaseLabels?.Any(l => l.Equals(_sourceBranch, StringComparison.OrdinalIgnoreCase));

				if (hasUpdateableLabel ?? false)
				{
					var updatedLabels = currentVersion.ReleaseLabels.Select(l => l.Replace(_sourceBranch, _targetBranch)).ToArray();

					var newVersion = new NuGetVersion(
						major: currentVersion.Major, 
						minor: currentVersion.Minor,
						patch: currentVersion.Patch, 
						revision: currentVersion.Revision, 
						releaseLabels: updatedLabels, 
						metadata: currentVersion.Metadata
					);

					updater(newVersion.ToFullString());

					LogUpdate(packageName, currentVersion, newVersion, doc.BaseURI);

					modified = true;
				}
			}

			return modified;
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