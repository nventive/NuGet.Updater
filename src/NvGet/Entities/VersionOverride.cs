using System;
using System.Collections.Generic;
using System.Text;
using NuGet.Versioning;

namespace NvGet.Entities
{
	public class VersionOverride
	{
		public VersionOverride(string packageId, string versionTag, VersionRange range)
		{
			PackageId = packageId;
			VersionTag = versionTag;
			Range = range;
		}

		public VersionOverride(string packageId, string versionTag, NuGetVersion version)
		{
			PackageId = packageId;
			VersionTag = versionTag;
			Version = version;
		}

		public string PackageId { get; }

		public string VersionTag { get; }

		public VersionRange Range { get; }

		public NuGetVersion Version { get; }

		public bool IsFixedVersion => Version != default;
	}
}
