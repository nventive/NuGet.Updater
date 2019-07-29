using System;
using NuGet.Versioning;

namespace NuGet.Updater.Entities
{
	public class UpdaterVersion
	{ 
		public UpdaterVersion(Uri feedUri, NuGetVersion version)
		{
			FeedUri = feedUri;
			Version = version;
		}

		public Uri FeedUri { get; }

		public NuGetVersion Version { get; }
	}
}
