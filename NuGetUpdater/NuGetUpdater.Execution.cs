using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nuget.Updater.Entities;

namespace Nuget.Updater
{
	partial class NuGetUpdater
	{
		public static bool Update(
			string solutionRoot,
			string sourceFeed,
			string targetVersion,
			string excludeTag = "",
			string PAT = "",
			bool includeNuGetOrg = true,
			bool allowDowngrade = false,
			bool strict = true,
			IEnumerable<string> keepLatestDev = null,
			IEnumerable<string> ignorePackages = null,
			IEnumerable<string> updatePackages = null,
			UpdateTarget target = UpdateTarget.All,
			Action<string> logAction = null,
			string summaryOutputFilePath = null
		)
		{
			return UpdateAsync(
				CancellationToken.None,
				solutionRoot,
				sourceFeed,
				targetVersion,
				excludeTag,
				PAT,
				includeNuGetOrg,
				allowDowngrade,
				strict,
				keepLatestDev,
				ignorePackages,
				updatePackages,
				target,
				logAction,
				summaryOutputFilePath
			).Result;
		}

		public static async Task<bool> UpdateAsync(
			CancellationToken ct,
			string solutionRoot,
			string sourceFeed,
			string targetVersion,
			string excludeTag = "",
			string feedPat = "",
			bool includeNuGetOrg = true,
			bool isDowngradeAllowed = false,
			bool strict = true,
			IEnumerable<string> packagesTokeepAtLatestDev = null,
			IEnumerable<string> packagesToIgnore = null,
			IEnumerable<string> packagesToUpdate = null,
			UpdateTarget updateTarget = UpdateTarget.All,
			Action<string> logAction = null,
			string summaryOutputFilePath = null
		)
		{
			var parameters = new Parameters
			{
				SolutionRoot = solutionRoot,
				SourceFeed = sourceFeed,
				SourceFeedPersonalAccessToken = feedPat,
				TargetVersion = targetVersion,
				Strict = strict,
				TagToExclude = excludeTag,
				UpdateTarget = updateTarget,
				IncludeNuGetOrg = includeNuGetOrg,
				IsDowngradeAllowed = isDowngradeAllowed,
				PackagesToKeepAtLatestDev = packagesTokeepAtLatestDev,
				PackagesToIgnore = packagesToIgnore,
				PackagesToUpdate = packagesToUpdate,
			};

			var log = new Logger(logAction, summaryOutputFilePath);

			return await UpdateAsync(ct, parameters, log);
		}

		public static async Task<bool> UpdateAsync(
			CancellationToken ct,
			Parameters parameters,
			Logger log
		)
		{
			var updater = new NuGetUpdater(parameters, log);
			return await updater.UpdatePackages(ct);
		}
	}
}
