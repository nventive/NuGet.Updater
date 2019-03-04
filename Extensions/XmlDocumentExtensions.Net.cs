﻿#if !UAP
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Nuget.Updater.Entities;
using NuGet.Versioning;

namespace Nuget.Updater.Extensions
{
	partial class XmlDocumentExtensions
	{
		private const string MsBuildNamespace = "http://schemas.microsoft.com/developer/msbuild/2003";

		public static UpdateOperation[] UpdateProjectReferenceVersions(
			this XmlDocument document,
			string packageId,
			FeedNuGetVersion version,
			string path,
			bool isDowngradeAllowed)
		{
			var operations = new List<UpdateOperation>();

			var nsmgr = new XmlNamespaceManager(document.NameTable);
			nsmgr.AddNamespace("d", MsBuildNamespace);
			operations.AddRange(document.UpdateProjectReferenceVersions(packageId, version, path, nsmgr, isDowngradeAllowed));

			var nsmgr2 = new XmlNamespaceManager(document.NameTable);
			nsmgr2.AddNamespace("d", "");
			operations.AddRange(document.UpdateProjectReferenceVersions(packageId, version, path, nsmgr2, isDowngradeAllowed));

			return operations.ToArray();
		}

		private static UpdateOperation[] UpdateProjectReferenceVersions(
			this XmlDocument document,
			string packageId,
			FeedNuGetVersion version,
			string path,
			XmlNamespaceManager namespaceManager,
			bool isDowngradeAllowed
		)
		{
			var operations = new List<UpdateOperation>();

			foreach (XmlElement packageReference in document.SelectNodes($"//d:PackageReference[@Include='{packageId}' or @Updated='{packageId}']", namespaceManager))
			{
				if (packageReference.HasAttribute("Version"))
				{
					var currentVersion = new NuGetVersion(packageReference.Attributes["Version"].Value);

					var operation = new UpdateOperation(isDowngradeAllowed, packageId, currentVersion, version, path);

					if (operation.ShouldProceed)
					{
						packageReference.SetAttribute("Version", version.Version.ToString());
					}

					operations.Add(operation);
				}
				else
				{
					var node = packageReference.SelectSingleNode("d:Version", namespaceManager);

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

		private static IEnumerable<XmlElement> GetElements(this XmlDocument document, string xpath)
		{
			var namespaceManager = new XmlNamespaceManager(document.NameTable);
			namespaceManager.AddNamespace("x", document.DocumentElement.NamespaceURI);

			return document
				.SelectNodes(xpath, namespaceManager)
				.OfType<XmlElement>();
		}

		public static async Task<KeyValuePair<string, XmlDocument>> GetDocument(this string path, CancellationToken ct)
		{
			var document = new XmlDocument()
			{
				PreserveWhitespace = true
			};

			document.Load(path);

			return new KeyValuePair<string, XmlDocument>(path, document);
		}

		public static async Task Save(this XmlDocument document, CancellationToken ct, string path)
		{
			document.Save(path);
		}
	}
}
#endif