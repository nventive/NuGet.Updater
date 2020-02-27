using System;
using System.Collections.Generic;
using System.Text;
using NeoGet.Tools.Tests.Entities;

namespace NeoGet.Tests
{
	public static class Constants
	{
		public static readonly Uri TestFeedUri = new Uri("http://localhost");

		public static readonly Dictionary<string, string[]> TestPackages = new Dictionary<string, string[]>
		{
			{"nventive.NuGet.Updater", new[] { "1.0-beta.1" } },
			{"Uno.UI", new[] { "2.1.39", "2.2.0", "2.3.0-dev.44", "2.3.0-dev.48", "2.3.0-dev.58" } },
		};

		public static readonly TestPackageFeed TestFeed = new TestPackageFeed(TestFeedUri, TestPackages);
	}
}
