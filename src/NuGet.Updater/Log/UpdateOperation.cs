using System;
using NuGet.Updater.Entities;
using NuGet.Versioning;

namespace NuGet.Updater.Log
{
	public class UpdateOperation
	{
		public UpdateOperation(bool isDowngradeAllowed, string packageName, NuGetVersion previousVersion, UpdaterVersion updatedVersion, string filePath)
		{
			Date = DateTimeOffset.Now;

			PackageName = packageName;
			PreviousVersion = previousVersion;
			UpdatedVersion = updatedVersion.Version;
			FilePath = filePath;
			FeedUri = updatedVersion.FeedUri;

			IsLatestVersion = PreviousVersion == UpdatedVersion;
			IsDowngrade = PreviousVersion.IsGreaterThan(UpdatedVersion) && isDowngradeAllowed;
			IsUpdate = UpdatedVersion.IsGreaterThan(PreviousVersion) || (!IsLatestVersion && isDowngradeAllowed);
		}

		public DateTimeOffset Date { get; }

		public string PackageName { get; }

		public NuGetVersion PreviousVersion { get; }

		public NuGetVersion UpdatedVersion { get; }

		public string FilePath { get; }

		public Uri FeedUri { get; }

		public bool IsUpdate { get; }

		public bool IsLatestVersion { get; }

		public bool IsDowngrade { get; }

		public string GetLogMessage()
		{
			if(IsLatestVersion)
			{
				return $"Version [{UpdatedVersion}] of [{PackageName}] already found in [{FilePath}]. Skipping.";
			}
			else if(IsDowngrade)
			{
				return $"Downgrading [{PackageName}] from [{PreviousVersion}] to [{UpdatedVersion}] in [{FilePath}]";
			}
			else if(IsUpdate)
			{
				return $"Updating [{PackageName}] from [{PreviousVersion}] to [{UpdatedVersion}] in [{FilePath}]";
			}
			else
			{ 
				return $"Higher verson of [{PackageName}] ([{UpdatedVersion}]) found in [{FilePath}]. Skipping.";
			}
		}
	}
}
