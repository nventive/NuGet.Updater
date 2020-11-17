using NuGet.Packaging.Core;

namespace NvGet.Entities
{
	public class LocalPackage
	{
		public LocalPackage()
		{
		}

		public LocalPackage(PackageIdentity identity, string path)
		{
			Identity = identity;
			Path = path;
		}

		public PackageIdentity Identity { get; }

		public string Path { get; }
	}
}
