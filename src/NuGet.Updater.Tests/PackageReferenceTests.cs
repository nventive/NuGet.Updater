using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NuGet.Shared.Entities;
using NuGet.Updater.Entities;
using NuGet.Updater.Extensions;
using NuGet.Updater.Tests.Entities;

namespace NuGet.Updater.Tests
{
	[TestClass]
	public class PackageReferenceTests
	{
		private static readonly Uri TestFeedUri = new Uri("http://localhost");

		private static readonly Dictionary<string, string[]> TestPackages = new Dictionary<string, string[]>
		{
			{"nventive.NuGet.Updater", new[] { "1.0-beta.1" } },
		};

		private static readonly TestPackageFeed TestFeed = new TestPackageFeed(TestFeedUri, TestPackages);

		[TestMethod]
		public async Task GivenPackageWithMatchingVersion_VersionIsFound()
		{
			var parameters = new UpdaterParameters
			{
				TargetVersions = new[] { "beta" },
				Feeds = new[] { TestFeed },
			};

			var packageVersion = "1.0-beta.1";
			var packageId = "nventive.NuGet.Updater";

			var reference = new PackageReference(packageId, packageVersion);

			var version = await reference.GetLatestVersion(CancellationToken.None, parameters);

			Assert.IsNotNull(version);
			Assert.AreEqual(version.Version.OriginalVersion, packageVersion);
		}

		[TestMethod]
		public async Task GivenPackageWithNoMatchingVersion_NoVersionIsFound()
		{
			var parameters = new UpdaterParameters
			{
				TargetVersions = new[] { "stable" },
			};

			var packageVersion = "1.0-beta.1";
			var packageId = "nventive.NuGet.Updater";

			var reference = new PackageReference(packageId, packageVersion);

			var version = await reference.GetLatestVersion(CancellationToken.None, parameters);

			Assert.IsNull(version);
		}
	}
}
