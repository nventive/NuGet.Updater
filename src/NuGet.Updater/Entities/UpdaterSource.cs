using System;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Configuration;
using NuGet.Updater.Extensions;
using NuGet.Updater.Log;

namespace NuGet.Updater.Entities
{
	public class UpdaterSource : IUpdaterSource
	{
		public static readonly UpdaterSource NuGetOrg = new UpdaterSource("https://api.nuget.org/v3/index.json");

		private readonly PackageSource _packageSource;
		private readonly bool _isPrivate = false;

		public UpdaterSource(string url)
		{
			_packageSource = new PackageSource(url);
		}

		public UpdaterSource(string url, string accessToken)
		{
			_packageSource = GetPackageSource(url, accessToken);
			_isPrivate = true;
		}

		public Uri Url => _packageSource.SourceUri;

		public Task<UpdaterPackage> GetPackage(
			CancellationToken ct,
			PackageReference reference,
			string author,
			Logger log = null
		) => _packageSource.GetPackage(ct, reference, author: _isPrivate ? null : author, log); //Not filtering packages from private sources

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
