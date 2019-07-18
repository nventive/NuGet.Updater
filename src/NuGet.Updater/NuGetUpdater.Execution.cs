using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Updater.Entities;

namespace NuGet.Updater
{
	/// <summary>
	/// Static execution methods for the NuGetUpdater.
	/// </summary>
	public partial class NuGetUpdater
	{
		public static bool Update(
			string solutionRoot,
			string sourceFeed,
			string targetVersion,
			string excludeTag = "",
			string feedAccessToken = "",
			bool includeNuGetOrg = true,
			string publicPackageOwner = null,
			bool allowDowngrade = false,
			bool strict = true,
			IEnumerable<string> keepLatestDev = null,
			IEnumerable<string> ignorePackages = null,
			IEnumerable<string> updatePackages = null,
			UpdateTarget target = UpdateTarget.All,
			TextWriter logWriter = null,
			string summaryOutputFilePath = null,
			bool useStableIfMoreRecent = false
		) => UpdateAsync(
				CancellationToken.None,
				solutionRoot,
				sourceFeed,
				targetVersion,
				excludeTag,
				feedAccessToken,
				includeNuGetOrg,
				publicPackageOwner,
				allowDowngrade,
				strict,
				keepLatestDev,
				ignorePackages,
				updatePackages,
				target,
				logWriter,
				summaryOutputFilePath,
				useStableIfMoreRecent
			).Result;

		public static bool Update(
			UpdaterParameters parameters,
			TextWriter logWriter = null,
			string summaryOutputFilePath = null
		) => UpdateAsync(CancellationToken.None, parameters, new Logger(logWriter, summaryOutputFilePath)).Result;

		public static async Task<bool> UpdateAsync(
			CancellationToken ct,
			string solutionRoot,
			string sourceFeed,
			string targetVersion,
			string excludeTag = "",
			string feedAccessToken = "",
			bool includeNuGetOrg = true,
			string publicPackageOwner = null,
			bool isDowngradeAllowed = false,
			bool strict = true,
			IEnumerable<string> packagesTokeepAtLatestDev = null,
			IEnumerable<string> packagesToIgnore = null,
			IEnumerable<string> packagesToUpdate = null,
			UpdateTarget updateTarget = UpdateTarget.All,
			TextWriter logWriter = null,
			string summaryOutputFilePath = null,
			bool useStableIfMoreRecent = false
		)
		{
			var parameters = new UpdaterParameters
			{
				SolutionRoot = solutionRoot,
				SourceFeed = sourceFeed,
				SourceFeedPersonalAccessToken = feedAccessToken,
				TargetVersion = targetVersion,
				Strict = strict,
				TagToExclude = excludeTag,
				UpdateTarget = updateTarget,
				IncludeNuGetOrg = includeNuGetOrg,
				PublickPackageOwner = publicPackageOwner,
				IsDowngradeAllowed = isDowngradeAllowed,
				PackagesToKeepAtLatestDev = packagesTokeepAtLatestDev,
				PackagesToIgnore = packagesToIgnore,
				PackagesToUpdate = packagesToUpdate,
				UseStableIfMoreRecent = useStableIfMoreRecent,
			};

			return await UpdateAsync(ct, parameters, logWriter, summaryOutputFilePath);
		}

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
			var updater = new NuGetUpdater(parameters, log);
			return await updater.UpdatePackages(ct);
		}
	}
}
