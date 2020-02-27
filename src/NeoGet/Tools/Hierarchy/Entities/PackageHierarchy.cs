using System.Collections.Generic;

namespace NeoGet.Tools.Hierarchy.Entities
{
	public class PackageHierarchy
	{
		public PackageHierarchy(string source, IEnumerable<PackageHierarchyItem> packages)
		{
			Source = source;
			Packages = new List<PackageHierarchyItem>(packages);
		}

		public string Source { get; set; }

		public List<PackageHierarchyItem> Packages { get; set; }
	}
}
