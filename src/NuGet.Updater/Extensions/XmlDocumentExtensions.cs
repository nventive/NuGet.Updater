using System.Collections.Generic;
using System.Linq;
using System.Xml;
using NuGet.Shared.Extensions;
using NuGet.Updater.Log;
using Uno.Extensions;

#if UAP
using XmlDocument = Windows.Data.Xml.Dom.XmlDocument;
using XmlElement = Windows.Data.Xml.Dom.XmlElement;
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
			var operations = new List<UpdateOperation>();

			var packageId = operation.PackageId;

			var packageReferences = document.SelectElements("PackageReference", $"[@Include='{packageId}' or @Update='{packageId}']");
			var dotnetCliReferences = document.SelectElements("DotNetCliToolReference", $"[@Include='{packageId}' or @Update='{packageId}']");

			foreach(var packageReference in packageReferences.Concat(dotnetCliReferences))
			{
				var packageVersion = packageReference.GetAttributeOrChild("Version");

				if(packageVersion.HasValue())
				{
					var currentOperation = operation.WithPreviousVersion(packageVersion);

					if(currentOperation.ShouldProceed())
					{
						packageReference.SetAttributeOrChild("Version", currentOperation.UpdatedVersion.ToString());
					}

					operations.Add(currentOperation);
				}
			}

			return operations;
		}

		/// <summary>
		/// Gets the attribute or child (in this order) of the given <see cref="XmlElement"/> with the given name.
		/// </summary>
		private static string GetAttributeOrChild(this XmlElement element, string name)
		{
			var attribute = element.GetAttribute(name);

			if(attribute.HasValue())
			{
				return attribute;
			}

			return element.SelectNode(name)?.InnerText;
		}

		/// <summary>
		/// Sets the attribute or child (in this order) of the given <see cref="XmlElement"/> with the given name.
		/// </summary>
		private static void SetAttributeOrChild(this XmlElement element, string name, string value)
		{
			var attribute = element.GetAttribute(name);

			if(attribute.HasValue())
			{
				element.SetAttribute(name, value);
			}
			else
			{
				var node = element.SelectNode(name);

				if(node != null)
				{
					node.InnerText = value;
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
			var operations = new List<UpdateOperation>();

			foreach(var node in document.SelectElements("dependency", $"[@id='{operation.PackageId}']"))
			{
				var versionNodeValue = node.GetAttribute("version");

				// only nodes with explicit version, skip expansion.
				if(!versionNodeValue.Contains("{", System.StringComparison.OrdinalIgnoreCase))
				{
					var currentOperation = operation.WithPreviousVersion(versionNodeValue);
					
					if(currentOperation.ShouldProceed())
					{
						node.SetAttribute("version", currentOperation.UpdatedVersion.ToString());
					}

					operations.Add(currentOperation);
				}
			}

			return operations;
		}
	}
}
