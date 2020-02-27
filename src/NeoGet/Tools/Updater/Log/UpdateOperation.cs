using System;
using NeoGet.Entities;
using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace NeoGet.Tools.Updater.Log
{
	public class UpdateOperation
	{
		public UpdateOperation(PackageIdentity identity, bool isIgnored)
			: this(identity.Id, identity.Version, null, null, null, canDowngrade: false)
		{
			IsIgnored = isIgnored;
		}

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
			CanDowngrade = canDowngrade;
			PackageId = packageId;
			PreviousVersion = previousVersion;
			UpdatedVersion = updatedVersion;
			FilePath = filePath;
			FeedUri = feedUri;
		}

		public bool IsIgnored { get; }

		public bool CanDowngrade { get; }

		public string PackageId { get; }

		public NuGetVersion PreviousVersion { get; }

		public NuGetVersion UpdatedVersion { get; }

		public string FilePath { get; }

		public Uri FeedUri { get; }

		public UpdateOperation WithPreviousVersion(string version) => new UpdateOperation(
			PackageId,
			previousVersion: new NuGetVersion(version),
			UpdatedVersion,
			FeedUri,
			FilePath,
			CanDowngrade
		);

		public UpdateOperation WithFilePath(string filePath) => new UpdateOperation(
			PackageId,
			PreviousVersion,
			UpdatedVersion,
			FeedUri,
			filePath,
			CanDowngrade
		);
	}
}
