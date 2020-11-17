using System.Collections.Generic;
using NuGet.Packaging.Core;
using NvGet.Contracts;
using NuGet.Versioning;

namespace NvGet.Entities
{
	public class PackageReference
	{
		public PackageReference(string id, string version)
			: this(new PackageIdentity(id, new NuGetVersion(version)), new Dictionary<FileType, string[]>())
		{
		}

		public PackageReference(string id, NuGetVersion version, string file, FileType type)
			: this(new PackageIdentity(id, version), new Dictionary<FileType, string[]>() { { type, new[] { file } } })
		{
		}

		public PackageReference(PackageIdentity identity, Dictionary<FileType, string[]> files)
		{
			Identity = identity;
			Files = files;
		}

		/// <summary>
		/// Gets the identity of the package.
		/// </summary>
		public PackageIdentity Identity { get; }

		/// <summary>
		/// Gets the locations where the refernce is present.
		/// </summary>
		public Dictionary<FileType, string[]> Files { get; }

		public override string ToString() => Identity.ToString();
	}
}
