using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NvGet.Contracts;
using NvGet.Tools.Tests.Entities;
using NvGet.Tools.Updater;
using NvGet.Tools.Updater.Entities;
using NvGet.Tools.Updater.Log;
using Uno.Extensions;

namespace NvGet.Tests.Tools
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

		private static readonly TestPackageFeed TestFeed = new TestPackageFeed(new Uri("http://localhost"), TestPackages);

		[TestMethod]
		public async Task GivenUnspecifiedTarget_NoUpdateIsMade()
		{
			var parameters = new UpdaterParameters
			{
				SolutionRoot = "MySolution.sln",
				UpdateTarget = FileType.Unspecified,
				TargetVersions = { "stable" },
				Feeds = { TestFeed },
			};

			var logger = new UpdaterLogger(Console.Out);

			var updater = new NuGetUpdater(parameters, logger);

			await updater.UpdatePackages(CancellationToken.None);

			Assert.IsTrue(logger.GetUpdates().None());
		}
	}
}
