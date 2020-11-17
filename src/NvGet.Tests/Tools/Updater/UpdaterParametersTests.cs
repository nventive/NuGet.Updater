using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeoGet.Entities;
using NeoGet.Tools.Updater.Entities;
using NeoGet.Tools.Updater.Extensions;
using NuGet.Versioning;

namespace NeoGet.Tests.Tools.Updater
{
	[TestClass]
	public class UpdaterParametersTests
	{
		[TestMethod]
		public async Task GivenRangeOverrides_CorrectVersionsAreResolved()
		{
			var reference = new PackageReference("Uno.UI", "2.1.39");

			var parameters = new UpdaterParameters
			{
				TargetVersions = { "dev", "stable" },
				Feeds = { Constants.TestFeed },
				VersionOverrides =
				{
					{ reference.Identity.Id, (false, VersionRange.Parse("(,2.3.0-dev.48]")) },
				},
			};

			var version = await parameters.GetLatestVersion(CancellationToken.None, reference);

			Assert.AreEqual(NuGetVersion.Parse("2.3.0-dev.48"), version.Version);

			parameters.VersionOverrides["Uno.UI"] = (false, VersionRange.Parse("(,2.3.0-dev.48)"));

			version = await parameters.GetLatestVersion(CancellationToken.None, reference);

			Assert.AreEqual(NuGetVersion.Parse("2.3.0-dev.44"), version.Version);
		}

		[TestMethod]
		public async Task GivenRangeOverrides_CorrectVersionsAreResolved_AndTargetVersionIsHonored()
		{
			var reference = new PackageReference("Uno.UI", "2.1.39");

			var parameters = new UpdaterParameters
			{
				TargetVersions = { "stable" },
				Feeds = { Constants.TestFeed },
				VersionOverrides =
				{
					{ reference.Identity.Id, (false, VersionRange.Parse("(,2.3.0-dev.48]")) },
				},
			};

			var version = await parameters.GetLatestVersion(CancellationToken.None, reference);

			Assert.AreEqual(NuGetVersion.Parse("2.2.0"), version.Version);
		}
	}
}
