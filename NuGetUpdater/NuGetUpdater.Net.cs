#if !UAP
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

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
