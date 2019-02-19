#if UAP
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Nuget.Updater.Entities;
using NuGet.Versioning;
using Windows.Data.Xml.Dom;
using Windows.Storage;
using XmlDocument = Windows.Data.Xml.Dom.XmlDocument;
using XmlElement = Windows.Data.Xml.Dom.XmlElement;

namespace Nuget.Updater.Extensions
{
	partial class XmlDocumentExtensions
	{
		public static UpdateOperation[] UpdateProjectReferenceVersions(
			this XmlDocument document,
			string packageId,
			FeedNuGetVersion version,
			string path,
			bool isDowngradeAllowed
		)
		{
			var operations = new List<UpdateOperation>();

			var packageReferences = document.GetElementsByTagName("PackageReference")
				.Cast<XmlElement>()
				.Where(p => p.Attributes.GetNamedItem("Include")?.NodeValue?.ToString() == packageId);

			foreach (var packageReference in packageReferences)
			{
				string versionAttribute = packageReference.GetAttribute("Version");

				if (versionAttribute != null && versionAttribute != "")
				{
					var currentVersion = new NuGetVersion(versionAttribute);

					var operation = new UpdateOperation(isDowngradeAllowed, packageId, currentVersion, version, path);

					if (operation.ShouldProceed)
					{
						packageReference.SetAttribute("Version", version.Version.ToString());
					}

					operations.Add(operation);
				}
				else
				{
					var node = packageReference.GetElementsByTagName("Version").SingleOrDefault();

					if (node != null)
					{
						var currentVersion = new NuGetVersion(node.InnerText);

						var operation = new UpdateOperation(isDowngradeAllowed, packageId, currentVersion, version, path);

						if (operation.ShouldProceed)
						{
							node.InnerText = version.Version.ToString();
						}

						operations.Add(operation);
					}
				}
			}

			return operations.ToArray();
		}

		private static IEnumerable<XmlElement> GetElements(this XmlDocument document, string xpath) => document
			.SelectNodes(xpath)
			.OfType<XmlElement>();

		public static async Task<KeyValuePair<string, XmlDocument>> GetDocument(this string path, CancellationToken ct)
		{
			var document = await XmlDocument.LoadFromFileAsync(await StorageFile.GetFileFromPathAsync(path), new XmlLoadSettings { ElementContentWhiteSpace = true });

			return new KeyValuePair<string, XmlDocument>(path, document);
		}

		public static async Task Save(this XmlDocument document, CancellationToken ct, string path)
		{
			await document.SaveToFileAsync(await StorageFile.GetFileFromPathAsync(path));
		}
	}
}
#endif