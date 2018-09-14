#if UAP
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Nuget.Updater.Entities;
using NuGet.Versioning;
using Windows.Data.Xml.Dom;
using Windows.Storage;
using Windows.Storage.Search;

namespace Nuget.Updater
{
	partial class NuGetUpdater
	{
		private static async Task<string[]> GetFiles(CancellationToken ct, string path, string extensionFilter = null, string nameFilter = null)
		{
			var folder = await StorageFolder.GetFolderFromPathAsync(path);

			var searchFilter = extensionFilter == null
				? $"filename:\"{nameFilter}\""
				: $"extension:{extensionFilter}";

			var queryOptions = new QueryOptions
			{
				UserSearchFilter = searchFilter,
				IndexerOption = IndexerOption.UseIndexerWhenAvailable,
				FolderDepth = FolderDepth.Deep
			};

			var query = folder.CreateFileQueryWithOptions(queryOptions);

			var files = await query.GetFilesAsync();

			return files
				.Select(f =>
				{
					_logAction($"Found {f.Path}");
					return f.Path;
				})
				.ToArray();
		}

		private static bool UpdateProjectReferenceVersions(string packageName, NuGetVersion version, bool modified, XmlDocument doc, string documentPath)
		{
			var packageReferences = doc.GetElementsByTagName("PackageReference")
				.Cast<XmlElement>()
				.Where(p => p.Attributes.GetNamedItem("Include")?.NodeValue?.ToString() == packageName);

			foreach (var packageReference in packageReferences)
			{
				string versionAttribute = packageReference.GetAttribute("Version");

				if (versionAttribute != null && versionAttribute != "")
				{
					var currentVersion = new NuGetVersion(versionAttribute);

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
					var node = packageReference.GetElementsByTagName("Version").SingleOrDefault();

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

			return false;
		}

		private static async Task<XmlDocument> GetDocument(CancellationToken ct, string path)
		{
			return await XmlDocument.LoadFromFileAsync(await StorageFile.GetFileFromPathAsync(path), new XmlLoadSettings { ElementContentWhiteSpace = true });
		}

		private static async Task SaveDocument(CancellationToken ct, XmlDocument document, string path)
		{
			await document.SaveToFileAsync(await StorageFile.GetFileFromPathAsync(path));
		}

		private static async Task<string> ReadFileContent(CancellationToken ct, string path)
		{
			var file = await StorageFile.GetFileFromPathAsync(path);
			var lines = await FileIO.ReadLinesAsync(file);
			string content = string.Join(Environment.NewLine, lines);

			return content;
		}

		private static async Task SetFileContent(CancellationToken ct, string path, string content)
		{
			var file = await StorageFile.GetFileFromPathAsync(path);
			await FileIO.WriteTextAsync(file, content);
		}
	}
}
#endif