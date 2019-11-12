using NuGet.Updater.Entities;
using NuGet.Updater.Log;

namespace NuGet.Updater.Extensions
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
			PreviousVersion = operation.PreviousVersion.OriginalVersion,
			UpdatedVersion = operation.UpdatedVersion.OriginalVersion,
		};
	}
}
