using NvGet.Extensions;
using NvGet.Tools.Updater.Entities;
using NvGet.Tools.Updater.Log;

namespace NvGet.Tools.Updater.Extensions
{
	public static class UpdateOperationExtensions
	{
		public static UpdateOperation ToUpdateOperation(this UpdaterPackage package, bool canDowngrade) => new UpdateOperation(
			package.PackageId,
			package.Version,
			canDowngrade
		);

		public static UpdateResult ToUpdateResult(this UpdateOperation operation) => new UpdateResult
		{
			PackageId = operation.PackageId,
			OriginalVersion = operation.PreviousVersion.OriginalVersion,
			UpdatedVersion = operation.UpdatedVersion?.OriginalVersion,
		};

		public static bool ShouldProceed(this UpdateOperation operation)
			=> !operation.IsIgnored
			&& (operation.UpdatedVersion.IsGreaterThan(operation.PreviousVersion) || operation.IsDowngrade());

		public static bool IsDowngrade(this UpdateOperation operation) => operation.PreviousVersion.IsGreaterThan(operation.UpdatedVersion) && operation.CanDowngrade;

		public static string GetLogMessage(this UpdateOperation operation)
		{
			if(operation.IsIgnored)
			{
				return $"Ignoring {operation.PackageId}";
			}
			else if(operation.PreviousVersion == operation.UpdatedVersion)
			{
				return $"Skipping {operation.PackageId}: version {operation.UpdatedVersion} already found in {operation.FilePath}";
			}
			else if(operation.IsDowngrade())
			{
				return $"Downgrading {operation.PackageId} from {operation.PreviousVersion} to {operation.UpdatedVersion} in {operation.FilePath}";
			}
			else if(operation.ShouldProceed())
			{
				return $"Updating {operation.PackageId} from {operation.PreviousVersion} to {operation.UpdatedVersion} in {operation.FilePath}";
			}
			else
			{
				return $"Skipping {operation.PackageId}: version {operation.PreviousVersion} found in {operation.FilePath}, version {operation.UpdatedVersion} found in {operation.FeedUri}";
			}
		}
	}
}
