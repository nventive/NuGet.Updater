using System;
using System.Collections.Generic;
using System.IO;
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
		}

		public static bool Update(
			Parameters parameters,
			TextWriter logWriter = null,
			string summaryOutputFilePath = null
		) => UpdateAsync(CancellationToken.None, parameters, new Logger(logWriter, summaryOutputFilePath)).Result;

		public static async Task<bool> UpdateAsync(
			CancellationToken ct,
			string solutionRoot,
			string sourceFeed,
			string targetVersion,
			string excludeTag = "",
			string feedPat = "",
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
			Parameters parameters,
			TextWriter logWriter = null,
			string summaryOutputFilePath = null
		) => UpdateAsync(ct, parameters, new Logger(logWriter, summaryOutputFilePath));

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
