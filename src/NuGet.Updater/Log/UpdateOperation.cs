using System;
using System.IO;
using NuGet.Shared.Entities;
using NuGet.Shared.Extensions;
using NuGet.Updater.Entities;
using NuGet.Versioning;

namespace NuGet.Updater.Log
{
	public class UpdateOperation
	{
		private readonly bool _canDowngrade;

		public UpdateOperation(string packageId, FeedVersion updatedVersion, bool canDowngrade)
			: this(packageId, previousVersion: null, updatedVersion, filePath: null, canDowngrade)
		{
		}

		private UpdateOperation(string packageId, NuGetVersion previousVersion, FeedVersion updatedVersion, string filePath, bool canDowngrade)
			: this(packageId, previousVersion, updatedVersion.Version, updatedVersion.FeedUri, filePath, canDowngrade)
		{
		}

		private UpdateOperation(string packageId, NuGetVersion previousVersion, NuGetVersion updatedVersion, Uri feedUri, string filePath, bool canDowngrade)
		{
			_canDowngrade = canDowngrade;

			PackageId = packageId;
			PreviousVersion = previousVersion;
			UpdatedVersion = updatedVersion;
			FilePath = filePath;
			FeedUri = feedUri;
		}

		public string PackageId { get; }

		public NuGetVersion PreviousVersion { get; }

		public NuGetVersion UpdatedVersion { get; }

		public string FilePath { get; }

		public Uri FeedUri { get; }

		public bool ShouldProceed => UpdatedVersion.IsGreaterThan(PreviousVersion) || ShouldDowngrade;

		public bool ShouldDowngrade => PreviousVersion.IsGreaterThan(UpdatedVersion) && _canDowngrade;

		public string GetLogMessage()
		{
			if(PreviousVersion == UpdatedVersion)
			{
				return $"Version [{UpdatedVersion}] of [{PackageId}] already found in [{FilePath}]. Skipping.";
			}
			else if(ShouldDowngrade)
			{
				return $"Downgrading [{PackageId}] from [{PreviousVersion}] to [{UpdatedVersion}] in [{FilePath}]";
			}
			else if(ShouldProceed)
			{
				return $"Updating [{PackageId}] from [{PreviousVersion}] to [{UpdatedVersion}] in [{FilePath}]";
			}
			else
			{ 
				return $"Version [{PreviousVersion}] of [{PackageId}] found in [{FilePath}]. Higher than [{UpdatedVersion}]. Skipping.";
			}
		}

		public UpdateOperation WithPreviousVersion(string version) => new UpdateOperation(
			PackageId,
			previousVersion: new NuGetVersion(version),
			UpdatedVersion,
			FeedUri,
			FilePath,
			_canDowngrade
		);

		public UpdateOperation WithFilePath(string filePath) => new UpdateOperation(
			PackageId,
			PreviousVersion,
			UpdatedVersion,
			FeedUri,
			filePath,
			_canDowngrade
		);
	}
}
