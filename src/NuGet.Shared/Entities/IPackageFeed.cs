using System;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;

namespace NuGet.Shared.Entities
{
	public interface IPackageFeed
	{
		Uri Url { get; }

		bool IsPrivate { get; }

		Task<FeedVersion[]> GetPackageVersions(CancellationToken ct, PackageReference reference, string author = null, ILogger log = null);
	}
}
