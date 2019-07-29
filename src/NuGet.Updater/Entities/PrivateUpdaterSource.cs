using System.Threading;
using System.Threading.Tasks;
using NuGet.Configuration;
using NuGet.Updater.Extensions;
using NuGet.Updater.Log;

namespace NuGet.Updater.Entities
{
	public class PrivateUpdaterSource : IUpdaterSource
	{
		private readonly PackageSource _packageSource;

		public PrivateUpdaterSource(string url, string accessToken)
		{
			_packageSource = GetPackageSource(url, accessToken);
		}

		public Task<NuGetPackage[]> GetPackages(CancellationToken ct, Logger log = null) => _packageSource.SearchPackages(ct, log: log);

		private static PackageSource GetPackageSource(string url, string accessToken)
		{
			var name = url.GetHashCode().ToString();

			return new PackageSource(url, name)
			{
#if UAP
				Credentials = PackageSourceCredential.FromUserInput(name, "user", accessToken, false),
#else
				Credentials = PackageSourceCredential.FromUserInput(name, "user", accessToken, false, null),
#endif
			};
		}
	}
}
