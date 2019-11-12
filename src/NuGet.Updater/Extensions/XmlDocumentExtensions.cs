using System.Collections.Generic;
using System.Linq;
using NuGet.Shared.Extensions;
using NuGet.Updater.Log;
using Uno.Extensions;

#if UAP
using XmlDocument = Windows.Data.Xml.Dom.XmlDocument;
#else
using XmlDocument = System.Xml.XmlDocument;
#endif

namespace NuGet.Updater.Extensions
{
	public static class XmlDocumentExtensions
	{
		/// <summary>
		/// Runs an <see cref="UpdateOperation"/> on the PackageReferences contained in a <see cref="XmlDocument"/>.
		/// </summary>
		/// <param name="document"></param>
		/// <param name="operation"></param>
		/// <returns></returns>
		public static IEnumerable<UpdateOperation> UpdatePackageReferences(
			this XmlDocument document,
			UpdateOperation operation
		)
		{
			var packageId = operation.PackageId;

			var packageReferences = document.SelectElements("PackageReference", $"[@Include='{packageId}' or @Update='{packageId}']");
			var dotnetCliReferences = document.SelectElements("DotNetCliToolReference", $"[@Include='{packageId}' or @Update='{packageId}']");

			foreach(var packageReference in packageReferences.Concat(dotnetCliReferences))
			{
				var packageVersion = packageReference.GetAttribute("Version");

				if(packageVersion.HasValue())
				{
					operation = operation.WithPreviousVersion(packageVersion);

					if(operation.ShouldProceed)
					{
						packageReference.SetAttribute("Version", operation.UpdatedVersion.ToString());
					}

					yield return operation;
				}
				else
				{
					var node = packageReference.SelectNode("Version");

					if(node != null)
					{
						operation = operation.WithPreviousVersion(node.InnerText);

						if(operation.ShouldProceed)
						{
							node.InnerText = operation.UpdatedVersion.ToString();
						}

						yield return operation;
					}
				}
			}
		}

		/// <summary>
		/// Runs an <see cref="UpdateOperation"/> on the dependencies contained in a <see cref="XmlDocument"/> loaded from a .nuspec file.
		/// </summary>
		/// <param name="document"></param>
		/// <param name="operation"></param>
		/// <returns></returns>
		public static IEnumerable<UpdateOperation> UpdateDependencies(
			this XmlDocument document,
			UpdateOperation operation
		)
		{
			foreach(var node in document.SelectElements("dependency", $"[@id='{operation.PackageId}']"))
			{
				var versionNodeValue = node.GetAttribute("version");

				// only nodes with explicit version, skip expansion.
				if(!versionNodeValue.Contains("{", System.StringComparison.OrdinalIgnoreCase))
				{
					operation = operation.WithPreviousVersion(versionNodeValue);
					
					if(operation.ShouldProceed)
					{
						node.SetAttribute("version", operation.UpdatedVersion.ToString());
					}

					yield return operation;
				}
			}
		}
	}
}
