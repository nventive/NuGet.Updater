#if !WINDOWS_UWP
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions;

namespace NeoGet.Helpers
{
	public static class FileHelper
	{
		public static void LogToFile(string outputFilePath, string line)
		{
			using (var writer = File.AppendText(outputFilePath))
			{
				writer.WriteLine(line);
			}
		}

		public static async Task<string[]> GetFiles(CancellationToken ct, string path, string extensionFilter = null, string nameFilter = null)
		{
			var filter = extensionFilter != null
				? "*" + extensionFilter
				: null;

			if (nameFilter.HasValue())
			{
				filter = nameFilter;
			}

			return Directory.GetFiles(path, filter, SearchOption.AllDirectories);
		}

		public static async Task<string> ReadFileContent(CancellationToken ct, string path)
			=> File.ReadAllText(path);

		public static async Task SetFileContent(CancellationToken ct, string path, string content)
			=> File.WriteAllText(path, content, Encoding.UTF8);

		public static async Task<bool> IsDirectory(CancellationToken ct, string path)
			=> (File.GetAttributes(path) & FileAttributes.Directory) == FileAttributes.Directory;

		public static async Task<bool> Exists(string path) => File.Exists(path);
	}
}
#endif
