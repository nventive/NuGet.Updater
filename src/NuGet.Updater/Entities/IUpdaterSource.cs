using System.Threading;
using System.Threading.Tasks;
using NuGet.Updater.Log;

namespace NuGet.Updater.Entities
{
	public interface IUpdaterSource
	{
		Task<NuGetPackage[]> GetPackages(CancellationToken ct, Logger log = null);
	}
}
