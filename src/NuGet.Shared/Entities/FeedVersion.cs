using System;
using NuGet.Versioning;

namespace NuGet.Shared.Entities
{
	public class FeedVersion : IComparable<FeedVersion>
	{
		internal FeedVersion(string version, Uri feedUri)
			: this(new NuGetVersion(version), feedUri)
		{
		}

		public FeedVersion(NuGetVersion version, Uri feedUri)
		{
			Version = version;
			FeedUri = feedUri;
		}

		public NuGetVersion Version { get; }

		public Uri FeedUri { get; }

		public int CompareTo(FeedVersion other) => Version.CompareTo(other.Version);
	}
}
