using System.Threading;
using System.Threading.Tasks;
using NuGet.Configuration;
using NuGet.Updater.Extensions;
using NuGet.Updater.Log;

namespace NuGet.Updater.Entities
{
	public class PublicUpdaterSource : IUpdaterSource
	{
		private readonly PackageSource _packageSource;
		private readonly string _packageOwner;

		public PublicUpdaterSource(string url, string packageOwner)
		{
			_packageSource = new PackageSource(url);
			_packageOwner = packageOwner;
		}

		public Task<NuGetPackage[]> GetPackages(CancellationToken ct, Logger log = null) => _packageSource.SearchPackages(ct, $"owner:{_packageOwner}", log);
	}
}
