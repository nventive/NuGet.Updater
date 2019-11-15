using System;
using NuGet.Versioning;

namespace NuGet.Updater.Entities
{
	public class UpdateResult : IEquatable<UpdateResult>
	{
		public string PackageId { get; set; }

		public string OriginalVersion { get; set; }

		public string UpdatedVersion { get; set; }

		public override int GetHashCode() => PackageId?.GetHashCode() ?? 0;

		public bool Equals(UpdateResult other) => other == null
			|| (other.PackageId.Equals(PackageId, StringComparison.OrdinalIgnoreCase) && other.UpdatedVersion.Equals(UpdatedVersion, StringComparison.OrdinalIgnoreCase));
	}
}
