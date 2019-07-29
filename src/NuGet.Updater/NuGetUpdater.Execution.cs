using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Updater.Entities;
using NuGet.Updater.Extensions;
using NuGet.Updater.Log;

namespace NuGet.Updater
{
	/// <summary>
	/// Static execution methods for the NuGetUpdater.
	/// </summary>
	public partial class NuGetUpdater
	{
		public static Task<bool> UpdateAsync(
			CancellationToken ct,
			UpdaterParameters parameters,
			TextWriter logWriter = null,
			string summaryOutputFilePath = null
		) => UpdateAsync(ct, parameters, new Logger(logWriter, summaryOutputFilePath));

		public static async Task<bool> UpdateAsync(
			CancellationToken ct,
			UpdaterParameters parameters,
			Logger log
		)
		{
			var updater = new NuGetUpdater(parameters, parameters.GetSources(), log);
			return await updater.UpdatePackages(ct);
		}
	}
}
