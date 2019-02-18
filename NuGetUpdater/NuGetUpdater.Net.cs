#if !UAP
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Nuget.Updater.Entities;
using NuGet.Versioning;

namespace Nuget.Updater
{
	public partial class NuGetUpdater
	{
		private static void LogUpdateSummaryToFile(string outputFilePath)
		{
			try
			{
				using (var file = File.Open(outputFilePath, FileMode.Open | FileMode.Truncate, FileAccess.Write))
				using (var writer = new StreamWriter(file))
				{
					LogSummary(line => writer.WriteLine(line), includeUrl: true);
				}
			}
			catch (Exception ex)
			{
				Log($"Failed to write to {outputFilePath}. Reason : {ex.Message}");
			}
		}

		private static async Task<string[]> GetFiles(CancellationToken ct, string path, string extensionFilter = null, string nameFilter = null)
		{
			var filter = extensionFilter != null
				? "*" + extensionFilter
				: null;

			if (nameFilter != null && filter == null)
			{
				filter = nameFilter;
			}

			return Directory.GetFiles(path, filter, SearchOption.AllDirectories);
		}
		private static bool UpdateProjectReferenceVersions(string packageName, FeedNuGetVersion version, XmlDocument document, string documentPath)
		{
			var modified = false;

			var nsmgr = new XmlNamespaceManager(document.NameTable);
			nsmgr.AddNamespace("d", MsBuildNamespace);
			modified |= UpdateProjectReferenceVersions(packageName, version, document, documentPath, nsmgr);

			var nsmgr2 = new XmlNamespaceManager(document.NameTable);
			nsmgr2.AddNamespace("d", "");
			modified |= UpdateProjectReferenceVersions(packageName, version, document, documentPath, nsmgr2);

			return modified;
		}

		private static bool UpdateProjectReferenceVersions(
			string packageName,
			FeedNuGetVersion version,
			XmlDocument doc,
			string documentPath,
			XmlNamespaceManager namespaceManager
		)
		{
			var modified = false;
			foreach (XmlElement packageReference in doc.SelectNodes($"//d:PackageReference[@Include='{packageName}']", namespaceManager))
			{
				if (packageReference.HasAttribute("Version"))
				{
					var currentVersion = new NuGetVersion(packageReference.Attributes["Version"].Value);

					var operation = new UpdateOperation(_allowDowngrade, packageName, currentVersion, version, documentPath);

					if (operation.ShouldProceed)
					{
						packageReference.SetAttribute("Version", version.Version.ToString());
						modified = true;
					}

					Log(operation);
				}
				else
				{
					var node = packageReference.SelectSingleNode("d:Version", namespaceManager);

					if (node != null)
					{
						var currentVersion = new NuGetVersion(node.InnerText);

						var operation = new UpdateOperation(_allowDowngrade, packageName, currentVersion, version, documentPath);

						if (operation.ShouldProceed)
						{
							node.InnerText = version.Version.ToString();
							modified = true;
						}

						Log(operation);
					}
				}
			}

			return modified;
		}

		private static async Task<XmlDocument> GetDocument(CancellationToken ct, string path)
		{
			var document = new XmlDocument()
			{
				PreserveWhitespace = true
			};

			document.Load(path);

			return document;
		}

		private static async Task SaveDocument(CancellationToken ct, XmlDocument document, string path)
		{
			document.Save(path);
		}

		private static async Task<string> ReadFileContent(CancellationToken ct, string path)
		{
			return File.ReadAllText(path);
		}

		private static async Task SetFileContent(CancellationToken ct, string path, string content)
		{
			File.WriteAllText(path, content, Encoding.UTF8);
		}
	}
}
#endif
