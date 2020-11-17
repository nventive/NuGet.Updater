using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeoGet.Entities;
using NeoGet.Tools.Updater.Entities;
using NeoGet.Tools.Updater.Extensions;
using NuGet.Versioning;

namespace NeoGet.Tests
{
	[TestClass]
	public class PackageReferenceTests
	{
		[TestMethod]
		public async Task GivenPackageWithMatchingVersion_VersionIsFound()
		{
			var parameters = new UpdaterParameters
			{
				TargetVersions = { "beta" },
				Feeds = { Constants.TestFeed },
			};

			var packageVersion = "1.0-beta.1";
			var packageId = "nventive.NuGet.Updater";

			var reference = new PackageReference(packageId, packageVersion);

			var version = await parameters.GetLatestVersion(CancellationToken.None, reference);

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

			var version = await parameters.GetLatestVersion(CancellationToken.None, reference);

			Assert.IsNull(version);
		}

		[TestMethod]
		public async Task GivenManualUpdates_AndVersionNotInFeed_ManualVersionIsFound()
		{
			var reference = new PackageReference("nventive.NuGet.Updater", "1.0");

			var parameters = new UpdaterParameters
			{
				TargetVersions = { "stable" },
				Feeds = { Constants.TestFeed },
				VersionOverrides =
				{
					{ reference.Identity.Id, (true, new VersionRange(reference.Identity.Version, true, reference.Identity.Version, true)) },
				},
			};

			var version = await parameters.GetLatestVersion(CancellationToken.None, reference);

			Assert.AreEqual(version.Version, reference.Identity.Version);
		}
	}
}
