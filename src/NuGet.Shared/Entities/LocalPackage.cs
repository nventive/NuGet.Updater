using NuGet.Packaging.Core;

namespace NuGet.Shared.Entities
{
	public class LocalPackage
	{
		internal LocalPackage(PackageIdentity identity, string path)
		{
			Identity = identity;
			Path = path;
		}

		public PackageIdentity Identity { get; }

		public string Path { get; }
	}
}
