#if UAP
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Search;

namespace NuGet.Updater.Helpers
{
	public static class FileHelper
	{
		public static void LogToFile(string outputFilePath, IEnumerable<string> log)
		{
		}

		public static async Task<string[]> GetFiles(
			CancellationToken ct,
			string path,
			string extensionFilter = null, 
			string nameFilter = null
		)
		{
			var folder = await StorageFolder.GetFolderFromPathAsync(path);

			var searchFilter = extensionFilter == null
				? $"filename:\"{nameFilter}\""
				: $"extension:{extensionFilter}";

			var queryOptions = new QueryOptions
			{
				UserSearchFilter = searchFilter,
				IndexerOption = IndexerOption.UseIndexerWhenAvailable,
				FolderDepth = FolderDepth.Deep,
			};

			var query = folder.CreateFileQueryWithOptions(queryOptions);

			var files = await query.GetFilesAsync();

			return files.Select(f => f.Path).ToArray();
		}

		public static async Task<string> ReadFileContent(CancellationToken ct, string path)
		{
			var file = await StorageFile.GetFileFromPathAsync(path);
			var lines = await FileIO.ReadLinesAsync(file);
			string content = string.Join(Environment.NewLine, lines);

			return content;
		}

		public static async Task SetFileContent(CancellationToken ct, string path, string content)
		{
			var file = await StorageFile.GetFileFromPathAsync(path);
			await FileIO.WriteTextAsync(file, content);
		}
	}
}
#endif
