using System;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Updater.Log;

namespace NuGet.Updater.Entities
{
	public interface IUpdaterSource
	{
		Uri Url { get; }

		Task<UpdaterPackage> GetPackage(CancellationToken ct, PackageReference reference, string author, Logger log = null);
	}
}
