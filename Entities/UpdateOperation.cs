using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Versioning;

namespace Nuget.Updater.Entities
{
	public class UpdateOperation
	{
		private readonly bool _isDowngradeAllowed;

		public UpdateOperation(bool isDowngradeAllowed, string packageName, NuGetVersion previousVersion, FeedNuGetVersion updatedVersion, string filePath)
		{
			_isDowngradeAllowed = isDowngradeAllowed;

			Date = DateTimeOffset.Now;

			PackageName = packageName;
			PreviousVersion = previousVersion;
			UpdatedVersion = updatedVersion.Version;
			FilePath = filePath;
			FeedUri = updatedVersion.FeedUri;
		}

		public DateTimeOffset Date { get; }

		public string PackageName { get; }

		public NuGetVersion PreviousVersion { get; }

		public NuGetVersion UpdatedVersion { get; }

		public string FilePath { get; }

		public Uri FeedUri { get; }

		public bool ShouldProceed => PreviousVersion < UpdatedVersion || (_isDowngradeAllowed && PreviousVersion.IsGreaterThan(UpdatedVersion));

		public bool IsLatestVersion => PreviousVersion == UpdatedVersion;

		public string GetLogMessage()
		{
			if (ShouldProceed)
			{
				return $"Updating [{PackageName}] from [{PreviousVersion}] to [{UpdatedVersion}] in [{FilePath}]";
			}
			else
			{
				return IsLatestVersion
					? $"Version [{UpdatedVersion}] of [{PackageName}] already found in [{FilePath}]. Skipping."
					: $"Higher verson of [{PackageName}] ([{UpdatedVersion}]) found in [{FilePath}]. Skipping.";
			}
		}
	}
}
