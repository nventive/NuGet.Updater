using System;
using NuGet.Versioning;

namespace NvGet.Entities
{
	public class FeedVersion : IComparable<FeedVersion>
	{
		public FeedVersion(string version, Uri feedUri)
			: this(new NuGetVersion(version), feedUri)
		{
		}

		public FeedVersion(NuGetVersion version)
			: this(version, null)
		{
		}

		public FeedVersion(NuGetVersion version, Uri feedUri)
		{
			Version = version;
			FeedUri = feedUri;
		}

		public NuGetVersion Version { get; }

		public Uri FeedUri { get; }

		public bool IsOverride => FeedUri == null;

		public int CompareTo(FeedVersion other) => Version.CompareTo(other.Version);
	}
}
