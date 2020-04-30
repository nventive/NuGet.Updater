using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NuGet.Shared.Entities;
using NuGet.Updater.Entities;
using NuGet.Updater.Extensions;
using NuGet.Updater.Tests.Entities;
using NuGet.Versioning;

namespace NuGet.Updater.Tests
{
	[TestClass]
	public class PackageReferenceTests
	{
		private static readonly Uri TestFeedUri = new Uri("http://localhost");

		private static readonly Dictionary<string, string[]> TestPackages = new Dictionary<string, string[]>
		{
			{"nventive.NuGet.Updater", new[] { "1.0-beta.1" } },
			{"Uno.UI", new[] { "2.1.39", "2.2.0", "2.3.0-dev.44", "2.3.0-dev.48", "2.3.0-dev.58" } },
		};

		private static readonly TestPackageFeed TestFeed = new TestPackageFeed(TestFeedUri, TestPackages);

		[TestMethod]
		public async Task GivenPackageWithMatchingVersion_VersionIsFound()
		{
			var parameters = new UpdaterParameters
			{
				TargetVersions = { "beta" },
				Feeds = { TestFeed },
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
				TargetVersions = { "stable" },
			};

			var packageVersion = "1.0-beta.1";
			var packageId = "nventive.NuGet.Updater";

			var reference = new PackageReference(packageId, packageVersion);

			var version = await reference.GetLatestVersion(CancellationToken.None, parameters);

			Assert.IsNull(version);
		}

		[TestMethod]
		public async Task GivenManualUpdates_AndVersionNotInFeed_ManualVersionIsFound()
		{
			var reference = new PackageReference("nventive.NuGet.Updater", "1.0");

			var parameters = new UpdaterParameters
			{
				TargetVersions = { "stable" },
				Feeds = { TestFeed },
				VersionOverrides =
				{
					{ reference.Identity.Id, (true, new VersionRange(reference.Identity.Version, true, reference.Identity.Version, true)) },
				},
			};

			var version = await reference.GetLatestVersion(CancellationToken.None, parameters);

			Assert.AreEqual(version.Version, reference.Identity.Version);
		}

		[TestMethod]
		public async Task GivenRangeOverrides_CorrectVersionsAreResolved()
		{
			var reference = new PackageReference("Uno.UI", "2.1.39");

			var parameters = new UpdaterParameters
			{
				TargetVersions = { "dev", "stable" },
				Feeds = { TestFeed },
				VersionOverrides =
				{
					{ reference.Identity.Id, (false, VersionRange.Parse("(,2.3.0-dev.48]")) },
				},
			};

			var version = await reference.GetLatestVersion(CancellationToken.None, parameters);

			Assert.AreEqual(NuGetVersion.Parse("2.3.0-dev.48"), version.Version);

			parameters.VersionOverrides["Uno.UI"] = (false, VersionRange.Parse("(,2.3.0-dev.48)"));

			version = await reference.GetLatestVersion(CancellationToken.None, parameters);

			Assert.AreEqual(NuGetVersion.Parse("2.3.0-dev.44"), version.Version);
		}

		[TestMethod]
		public async Task GivenRangeOverrides_CorrectVersionsAreResolved_AndTargetVersionIsHonored()
		{
			var reference = new PackageReference("Uno.UI", "2.1.39");

			var parameters = new UpdaterParameters
			{
				TargetVersions = { "stable" },
				Feeds = { TestFeed },
				VersionOverrides =
				{
					{ reference.Identity.Id, (false, VersionRange.Parse("(,2.3.0-dev.48]")) },
				},
			};

			var version = await reference.GetLatestVersion(CancellationToken.None, parameters);

			Assert.AreEqual(NuGetVersion.Parse("2.2.0"), version.Version);
		}
	}
}
