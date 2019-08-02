using System;
using System.Collections.Generic;
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
		private static readonly Dictionary<string, string[]> TestPackages = new Dictionary<string, string[]>
		{
			{"Uno.UI", new[] { "1.0", "1.1-dev.1" } },
			{"Uno.Core", new[] { "1.0", "1.0-beta.1" } },
			{"nventive.NuGet.Updater", new[] { "1.0-beta.1" } },
		};

		private static readonly TestPackageSource TestSource = new TestPackageSource(new Uri("http://localhost"), TestPackages);

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
	}
}
