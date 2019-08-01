using System.Threading;
using System.Threading.Tasks;
using NuGet.Updater.Log;

namespace NuGet.Updater.Entities
{
	public interface IUpdaterSource
	{
		Task<UpdaterPackage[]> GetPackages(CancellationToken ct, Logger log = null);

		Task<UpdaterPackage> GetPackage(CancellationToken ct, PackageReference reference, Logger log = null);
	}
}
