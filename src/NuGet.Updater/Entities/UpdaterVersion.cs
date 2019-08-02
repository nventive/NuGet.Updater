using System;
using NuGet.Versioning;

namespace NuGet.Updater.Entities
{
	public class UpdaterVersion : IComparable<UpdaterVersion>
	{
		internal UpdaterVersion(string version, Uri feedUri)
			: this(new NuGetVersion(version), feedUri)
		{
		}

		public UpdaterVersion(NuGetVersion version, Uri feedUri)
		{
			Version = version;
			FeedUri = feedUri;
		}

		public NuGetVersion Version { get; }

		public Uri FeedUri { get; }

		public int CompareTo(UpdaterVersion other) => Version.CompareTo(other.Version);
	}
}
