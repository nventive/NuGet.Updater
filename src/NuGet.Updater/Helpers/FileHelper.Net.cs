#if !UAP
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NuGet.Updater.Helpers
{
	partial class FileHelper
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

		public static async Task<string> ReadFileContent(CancellationToken ct, string path)
		{
			return File.ReadAllText(path);
		}

		public static async Task SetFileContent(CancellationToken ct, string path, string content)
		{
			File.WriteAllText(path, content, Encoding.UTF8);
		}
	}
}
#endif
