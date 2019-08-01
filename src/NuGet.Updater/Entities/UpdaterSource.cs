using System;
using System.Collections.Generic;
using System.Text;
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
		private readonly string _packageOwner;

		public UpdaterSource(string url, string packageOwner)
		{
			_packageOwner = packageOwner;
			_packageSource = new PackageSource(url);
		}

		public UpdaterSource(string url, string accessToken, string packageOwner)
		{
			_packageOwner = packageOwner;
			_packageSource = GetPackageSource(url, accessToken);
		}

		public Task<UpdaterPackage> GetPackage(CancellationToken ct, PackageReference reference, Logger log = null) => _packageSource.GetPackage(ct, reference, log);

		public Task<UpdaterPackage[]> GetPackages(CancellationToken ct, Logger log = null) => _packageSource.SearchPackages(ct, log: log);

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
