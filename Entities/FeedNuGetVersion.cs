using System;
using NuGet.Versioning;

namespace Nuget.Updater.Entities
{
	public class FeedNuGetVersion
	{
		public FeedNuGetVersion(Uri feedUri, NuGetVersion version)
		{
			FeedUri = feedUri;
			Version = version;
		}

		public Uri FeedUri { get; }

		public NuGetVersion Version { get; }
	}
}
