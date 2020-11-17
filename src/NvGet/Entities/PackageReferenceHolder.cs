using System.Collections.Generic;
using System.IO;
using NuGet.Packaging.Core;

namespace NeoGet.Entities
{
	public class PackageReferenceHolder
	{
		public PackageReferenceHolder(string path, IEnumerable<PackageIdentity> packages)
		{
			Path = path;
			Packages = new List<PackageIdentity>(packages);

			Name = System.IO.Path.GetFileName(path);
		}

		public string Path { get; set; }

		public string Name { get; }

		public List<PackageIdentity> Packages { get; set; }
	}
}
