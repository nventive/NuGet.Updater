using System;
using NuGet.Versioning;

namespace NvGet.Entities
{
	public class FeedVersion : IComparable<FeedVersion>
	{
		public FeedVersion(NuGetVersion version, Uri feedUri = null, string versionTag = null)
		{
			Version = version;
			FeedUri = feedUri;
			VersionTag = versionTag;
		}

		public NuGetVersion Version { get; }

		public Uri FeedUri { get; }

		public string VersionTag { get; }

		public bool IsOverride => FeedUri == null;

		public int CompareTo(FeedVersion other) => Version.CompareTo(other.Version);
	}
}
