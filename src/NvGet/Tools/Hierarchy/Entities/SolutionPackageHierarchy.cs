using System.Collections.Generic;

namespace NvGet.Tools.Hierarchy.Entities
{
	public class SolutionPackageHierarchy
	{
		public SolutionPackageHierarchy(string name)
		{
			Name = name;
			Projects = new List<ProjectPackageHierarchy>();
		}

		public string Name { get; set; }

		public List<ProjectPackageHierarchy> Projects { get; }

		public override string ToString() => Name;
	}
}
