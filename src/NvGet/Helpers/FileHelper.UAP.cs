#if WINDOWS_UWP
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Search;

namespace NvGet.Helpers
{
	/// <summary>
	/// UAP-Specific methods.
	/// </summary>
	public class FileHelper
	{
		public static void LogToFile(string outputFilePath, string line)
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
			var file = await StorageFile.GetFileFromPathAsync(path).AsTask(ct);
			await FileIO.WriteTextAsync(file, content);
		}

		public static async Task<bool> IsDirectory(CancellationToken ct, string path)
		{
			try
			{
				var folder = await StorageFolder.GetFolderFromPathAsync(path).AsTask(ct);

				return true;
			}
			catch(ArgumentException)
			{
				return false;
			}
		}

		public static async Task<bool> Exists(string path)
		{
			try
			{
				var file = await StorageFile.GetFileFromPathAsync(path);
				return true;
			}
			catch(Exception)
			{
				return false;
			}
		}
	}
}
#endif
