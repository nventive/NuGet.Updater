using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NuGet.Updater.Entities;
using NuGet.Updater.Extensions;
using NuGet.Updater.Log;
using NuGet.Updater.Tests.Entities;
using Uno.Extensions;

namespace NuGet.Updater.Tests
{
	[TestClass]
	public class NuGetUpdaterTests
	{
		private static readonly TestPackageSource TestSource = new TestPackageSource(
			new Uri("http://localhost"),
			new TestPackage("Uno.UI", "1.0", "1.1-dev.1"),
			new TestPackage("Uno.Core", "1.0", "1.0-beta.1"),
			new TestPackage("nventive.NuGet.Updater", "1.0-beta.1")
		);

		[TestMethod]
		public async Task GivenUnspecifiedTarget_NoUpdateIsMade()
		{
			var parameters = new UpdaterParameters
			{
				SolutionRoot = "MySolution.sln",
				UpdateTarget = UpdateTarget.Unspecified,
				TargetVersions = new[] { "stable" },
			};

			var logger = new Logger(Console.Out);

			var updater = new NuGetUpdater(parameters, new[] { TestSource }, logger);

			await updater.UpdatePackages(CancellationToken.None);

			Assert.IsTrue(logger.GetUpdates().None());
		}

		[TestMethod]
		public async Task GivenParameters_PackagesAreFound()
		{
			var parameters = new UpdaterParameters
			{
				SolutionRoot = "MySolution.sln",
				UpdateTarget = UpdateTarget.DirectoryProps | UpdateTarget.DirectoryTargets,
				IncludeNuGetOrg = true,
				TargetVersions = new[] { "stable" },
			};

			var logger = new Logger(Console.Out);

			var updater = new NuGetUpdater(parameters, parameters.GetSources(), logger);

			var packages = await updater.GetPackages(CancellationToken.None);

			//foreach(var p in packages.Where(p => p.Versions.Any()))
			//{
			//}

			Assert.IsTrue(packages.Any());
		}
	}
}
