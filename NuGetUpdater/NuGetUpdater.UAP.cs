#if UAP
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;
using Windows.Storage;

namespace Nuget.Updater
{
	partial class NuGetUpdater
	{
		private static async Task<string[]> GetFiles(CancellationToken ct, string path, string extensionFilter = null, string nameFilter = null)
		{
			var folder = await StorageFolder.GetFolderFromPathAsync(path);

			var subFoldersContent = new List<string>();
			var folders = await folder.GetFoldersAsync();

			if (folders.Any())
			{
				foreach (var f in folders)
				{
					subFoldersContent.AddRange(await GetFiles(ct, f.Path, extensionFilter, nameFilter));
				}
			}

			var files = await folder.GetFilesAsync();

			return files
				.Where(f => (extensionFilter == null && nameFilter == null) || (extensionFilter != null && f.FileType == extensionFilter) || (nameFilter != null && f.Name == nameFilter))
				.Select(f => f.Path)
				.Concat(subFoldersContent)
				.ToArray();
		}

		private static async Task<XmlDocument> GetDocument(CancellationToken ct, string path)
		{
			return await XmlDocument.LoadFromFileAsync(await StorageFile.GetFileFromPathAsync(path));
		}

		private static async Task SaveDocument(CancellationToken ct, XmlDocument document, string path)
		{
			await document.SaveToFileAsync(await StorageFile.GetFileFromPathAsync(path));
		}

		private static async Task<string> ReadFileContent(CancellationToken ct, string path)
		{
			var file = await StorageFile.GetFileFromPathAsync(path);
			var lines = await FileIO.ReadLinesAsync(file);
			var content = string.Join(Environment.NewLine, lines);

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