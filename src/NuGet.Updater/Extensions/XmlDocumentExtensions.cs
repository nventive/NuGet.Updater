using System.Collections.Generic;
using System.Linq;
using NuGet.Shared.Entities;
using NuGet.Shared.Extensions;
using NuGet.Updater.Log;
using NuGet.Versioning;
using Uno.Extensions;

#if UAP
using System.Text.RegularExpressions;
using Windows.Data.Xml.Dom;
using Windows.Storage;
using XmlDocument = Windows.Data.Xml.Dom.XmlDocument;
using XmlElement = Windows.Data.Xml.Dom.XmlElement;
using XmlNode = Windows.Data.Xml.Dom.IXmlNode;
#else
using XmlDocument = System.Xml.XmlDocument;
#endif

namespace NuGet.Updater.Extensions
{
	public static class XmlDocumentExtensions
	{
		/// <summary>
		/// Updates the PackageReferences with the given id in the given XmlDocument.
		/// </summary>
		/// <param name="document"></param>
		/// <param name="packageId"></param>
		/// <param name="version"></param>
		/// <param name="path"></param>
		/// <param name="isDowngradeAllowed"></param>
		/// <returns></returns>
		public static UpdateOperation[] UpdatePackageReferences(
			this XmlDocument document,
			string packageId,
			FeedVersion version,
			string path,
			bool isDowngradeAllowed)
		{
			var operations = new List<UpdateOperation>();

			var packageReferences = document.SelectElements("PackageReference", $"[@Include='{packageId}' or @Update='{packageId}']");
			var dotnetCliReferences = document.SelectElements("DotNetCliToolReference", $"[@Include='{packageId}' or @Update='{packageId}']");

			foreach(var packageReference in packageReferences.Concat(dotnetCliReferences))
			{
				var packageVersion = packageReference.GetAttribute("Version");

				if(packageVersion.HasValue())
				{
					var currentVersion = new NuGetVersion(packageVersion);

					var operation = new UpdateOperation(isDowngradeAllowed, packageId, currentVersion, version, path);

					if(operation.IsUpdate)
					{
						packageReference.SetAttribute("Version", version.Version.ToString());
					}

					operations.Add(operation);
				}
				else
				{
					var node = packageReference.SelectNode("Version");

					if(node != null)
					{
						var currentVersion = new NuGetVersion(node.InnerText);

						var operation = new UpdateOperation(isDowngradeAllowed, packageId, currentVersion, version, path);

						if(operation.IsUpdate)
						{
							node.InnerText = version.Version.ToString();
						}

						operations.Add(operation);
					}
				}
			}

			return operations.ToArray();
		}

		/// <summary>
		/// Updates the dependencies with the given id in the given XmlDocument.
		/// </summary>
		/// <param name="document"></param>
		/// <param name="packageId"></param>
		/// <param name="version"></param>
		/// <param name="path"></param>
		/// <param name="isDowngradeAllowed"></param>
		/// <returns></returns>
		public static UpdateOperation[] UpdateDependencies(
			this XmlDocument document,
			string packageId,
			FeedVersion version,
			string path,
			bool isDowngradeAllowed
		)
		{
			var operations = new List<UpdateOperation>();

			foreach(var node in document.SelectElements("dependency", $"[@id='{packageId}']"))
			{
				var versionNodeValue = node.GetAttribute("version");

				// only nodes with explicit version, skip expansion.
				if(!versionNodeValue.Contains("{"))
				{
					var currentVersion = new NuGetVersion(versionNodeValue);

					var operation = new UpdateOperation(isDowngradeAllowed, packageId, currentVersion, version, path);

					if(operation.IsUpdate)
					{
						node.SetAttribute("version", version.Version.ToString());
					}

					operations.Add(operation);
				}
			}

			return operations.ToArray();
		}
	}
}
