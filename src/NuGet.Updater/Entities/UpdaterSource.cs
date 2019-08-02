using System.Threading;
using System.Threading.Tasks;
using NuGet.Configuration;
using NuGet.Updater.Extensions;
using NuGet.Updater.Log;

namespace NuGet.Updater.Entities
{
	public class UpdaterSource : IUpdaterSource
	{
		private readonly PackageSource _packageSource;
		private readonly string _packageAuthor;

		public UpdaterSource(string url, string packageOwner)
		{
			_packageAuthor = packageOwner;
			_packageSource = new PackageSource(url);
		}

		public UpdaterSource(string url, string accessToken, string packageAuthor)
		{
			//Not filtering packages from private feeds
			_packageAuthor = null;
			_packageSource = GetPackageSource(url, accessToken);
		}

		public Task<UpdaterPackage> GetPackage(
			CancellationToken ct,
			PackageReference reference,
			Logger log = null
		) => _packageSource.GetPackage(ct, reference, _packageAuthor, log);

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
