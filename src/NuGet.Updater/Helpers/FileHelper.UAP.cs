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
	partial class FileHelper
	{
		public static void LogToFile(string outputFilePath, IEnumerable<string> log)
		{
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
