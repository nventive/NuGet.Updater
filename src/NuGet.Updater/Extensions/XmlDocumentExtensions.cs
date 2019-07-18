using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Updater.Entities;
using NuGet.Versioning;

#if UAP
using XmlDocument = Windows.Data.Xml.Dom.XmlDocument;
#else
using XmlDocument = System.Xml.XmlDocument;
#endif

namespace NuGet.Updater.Extensions
{
	/// <summary>
	/// Shared XmlDocument extension methods.
	/// </summary>
	public static partial class XmlDocumentExtensions
	{
		public static UpdateOperation[] UpdateNuSpecVersions(
			this XmlDocument document,
			string packageId,
			FeedNuGetVersion version,
			string path,
			bool isDowngradeAllowed
		)
		{
			var operations = new List<UpdateOperation>();

			foreach (var node in document.GetElements($"//x:dependency[@id='{packageId}']"))
			{
				var versionNodeValue = node.GetAttribute("version");

				// only nodes with explicit version, skip expansion.
				if (!versionNodeValue.Contains("{"))
				{
					var currentVersion = new NuGetVersion(versionNodeValue);

					var operation = new UpdateOperation(isDowngradeAllowed, packageId, currentVersion, version, path);

					if (operation.ShouldProceed)
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
