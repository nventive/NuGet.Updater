#if !UAP
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Nuget.Updater.Entities;
using NuGet.Versioning;

namespace Nuget.Updater
{
	partial class NuGetUpdater
	{

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

		private static bool UpdateProjectReferenceVersions(string packageName, NuGetVersion version, bool modified, XmlDocument doc, string documentPath, XmlNamespaceManager namespaceManager)
	{
			foreach (XmlElement packageReference in doc.SelectNodes($"//d:PackageReference[@Include='{packageName}']"))
			{
				if (packageReference.HasAttribute("Version"))
				{
					var currentVersion = new NuGetVersion(packageReference.Attributes["Version"].Value);

					var operation = new UpdateOperation(_allowDowngrade, packageName, currentVersion, version, documentPath);

					if (operation.ShouldProceed)
					{
						packageReference.SetAttribute("Version", version.ToString());
						modified = true;
					}

					_logAction(operation.GetLogMessage());
					_updateOperations.Add(operation);
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
							node.InnerText = version.ToString();
							modified = true;
						}

						_logAction(operation.GetLogMessage());
						_updateOperations.Add(operation);
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
