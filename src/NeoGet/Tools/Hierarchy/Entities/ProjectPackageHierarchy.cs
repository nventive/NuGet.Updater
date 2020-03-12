using System.Collections.Generic;

namespace NeoGet.Tools.Hierarchy.Entities
{
	public class ProjectPackageHierarchy
	{
		public ProjectPackageHierarchy(string name, IEnumerable<PackageHierarchyItem> packages)
		{
			Name = name;
			Packages = new List<PackageHierarchyItem>(packages);
		}

		public string Name { get; set; }

		public List<PackageHierarchyItem> Packages { get; set; }

		public override string ToString() => Name;
	}
}
