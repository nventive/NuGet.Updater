using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NuGet.Updater.Entities;
using NuGet.Updater.Extensions;
using NuGet.Updater.Tests.Entities;

namespace NuGet.Updater.Tests
{
	[TestClass]
	public class UpdaterPackagePackageTests
	{
		[TestMethod]
		public async Task GivenPackageWithMatchingVersion_VersionIsFound()
		{
			var parameters = new UpdaterParameters
			{
				TargetVersions = new[] { "beta" },
			};

			var packageVersion = "1.0-beta.1";
			var package = new UpdaterPackage(new TestPackage("nventive.NuGet.Updater", packageVersion), new Uri("http://localhost"));

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

			var package = new UpdaterPackage(new TestPackage("nventive.NuGet.Updater", "1.0-beta.1"), new Uri("http://localhost"));

			var version = package.GetLatestVersion(parameters);

			Assert.IsNull(version);
		}
	}
}
