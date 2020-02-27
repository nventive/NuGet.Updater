using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Shared.Entities;
using NuGet.Updater.Entities;

namespace NuGet.Updater.Extensions
{
	public static class FeedVersionExtensions
	{
		/// <summary>
		/// Gets the latest version for the given reference by looking up first in a list of known packages.
		/// Useful in the cases where refernces to multiple versions of the same packages are found.
		/// </summary>
		public static async Task<FeedVersion> GetLatestVersion(
			this PackageReference reference,
			CancellationToken ct,
			IEnumerable<UpdaterPackage> knownPackages,
			UpdaterParameters parameters
		)
		{
			var knownVersion = knownPackages.FirstOrDefault(p => p.PackageId == reference.Identity.Id)?.Version;

			if(knownVersion == null)
			{
				knownVersion = await reference.GetLatestVersion(ct, parameters);
			}

			return knownVersion;
		}
	}
}
