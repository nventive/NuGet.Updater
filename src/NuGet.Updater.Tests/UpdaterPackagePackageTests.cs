using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NuGet.Updater.Entities;
using NuGet.Updater.Extensions;

namespace NuGet.Updater.Tests
{
	[TestClass]
	public class UpdaterPackagePackageTests
	{
		private static readonly Uri TestFeedUri = new Uri("http://localhost");

		[TestMethod]
		public async Task GivenPackageWithMatchingVersion_VersionIsFound()
		{
			var parameters = new UpdaterParameters
			{
				TargetVersions = new[] { "beta" },
			};

			var packageVersion = "1.0-beta.1";
			var package = new UpdaterPackage("nventive.NuGet.Updater", TestFeedUri, packageVersion);

			var version = package.GetLatestVersion(parameters);

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

			var package = new UpdaterPackage("nventive.NuGet.Updater", TestFeedUri, "1.0-beta.1");

			var version = package.GetLatestVersion(parameters);

			Assert.IsNull(version);
		}
	}
}
