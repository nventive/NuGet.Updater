#if !UAP
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NuGet.Shared.Helpers
{
	internal class FileHelper
	{
		public static void LogToFile(string outputFilePath, IEnumerable<string> log)
		{
			if (File.Exists(outputFilePath))
			{
				File.WriteAllText(outputFilePath, "");
			}

			using (var file = File.OpenWrite(outputFilePath))
			using (var writer = new StreamWriter(file))
			{
				foreach (var line in log)
				{
					writer.WriteLine(line);
				}
			}
		}

		public static async Task<string[]> GetFiles(CancellationToken ct, string path, string extensionFilter = null, string nameFilter = null)
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

		public static async Task<string> ReadFileContent(CancellationToken ct, string path)
		{
			return File.ReadAllText(path);
		}

		public static async Task SetFileContent(CancellationToken ct, string path, string content)
		{
			File.WriteAllText(path, content, Encoding.UTF8);
		}

		public static async Task<bool> IsDirectory(CancellationToken ct, string path)
		{
			return (File.GetAttributes(path) & FileAttributes.Directory) == FileAttributes.Directory;
		}
	}
}
#endif
