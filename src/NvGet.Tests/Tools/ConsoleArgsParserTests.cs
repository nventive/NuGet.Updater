using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NvGet.Entities;
using NvGet.Tools.Arguments;
using NvGet.Tools.Updater.Entities;

namespace NvGet.Tests.Tools
{
	[TestClass]
	public class ConsoleArgsParserTests
	{
		private const string NotExistingFilePath = @"c:\not\existing\file.mia";
		private const string SomeText = nameof(SomeText);
		private const string SomePublicFeed = "https://pkgs.dev.azure.com/qwe/_packaging/asd/nuget/v3/index.json";
		private const string SomePrivateFeed = "https://pkgs.dev.azure.com/qwe/_packaging/asd/nuget/v3/index.json|hunter2";
		private const string PinnedVersionJsonPath = @"Resources\version_overrides.json";
		
		[TestMethod]
		public void Given_HelpArgument_ContextIsHelp()
		{
			var arguments = new[] { "-help" };
			var context = ConsoleArgsContext.Parse(arguments);

			Assert.IsTrue(context.IsHelp);
		}

		[TestMethod]
		public void Given_UnrecognizedArgument_ContextHasError()
		{
			var arguments = new[] { "--absolutelyWrong" };
			var context = ConsoleArgsContext.Parse(arguments);

			Assert.IsTrue(context.HasError);
			Assert.AreEqual(context.Errors[0].Argument, arguments[0]);
			Assert.AreEqual(context.Errors[0].Type, ConsoleArgErrorType.UnrecognizedArgument);
		}

		[TestMethod]
		[Ignore("fixme: Mono.Options recognizes `- asd` because it is parsed as parameter `-a` with a value of `sd`")]
		public void Given_UnrecognizedArgument_ContextHasError2()
		{
			var arguments = new[] { "-asd" };
			var context = ConsoleArgsContext.Parse(arguments);

			Assert.IsTrue(context.HasError);
			Assert.AreEqual(context.Errors[0].Argument, arguments[0]);
			Assert.AreEqual(context.Errors[0].Type, ConsoleArgErrorType.UnrecognizedArgument);
		}

		[TestMethod]
		public void Given_InvalidArgumentParameter_ContextHasError()
		{
			var arguments = new[] { $"--versionOverrides={NotExistingFilePath}" };
			var context = ConsoleArgsContext.Parse(arguments);

			Assert.IsTrue(context.HasError);
			Assert.AreEqual(context.Errors[0].Argument, NotExistingFilePath);
			Assert.AreEqual(context.Errors[0].Type, ConsoleArgErrorType.ValueParsingError);
			Assert.IsInstanceOfType(context.Errors[0].Exception, typeof(DirectoryNotFoundException));
		}

		[DataTestMethod]
		[DataRow(nameof(ConsoleArgsContext.IsHelp), "--help", true)]
		[DataRow(nameof(ConsoleArgsContext.IsHelp), "-h", true)]
		[DataRow(nameof(ConsoleArgsContext.IsSilent), "--silent", true)]
		[DataRow(nameof(ConsoleArgsContext.ResultFile), "--result=" + NotExistingFilePath, NotExistingFilePath)]
		[DataRow(nameof(ConsoleArgsContext.ResultFile), "-r=" + NotExistingFilePath, NotExistingFilePath)]
		[DataRow(nameof(ConsoleArgsContext.SummaryFile), "--outputFile=" + NotExistingFilePath, NotExistingFilePath)]
		[DataRow(nameof(ConsoleArgsContext.SummaryFile), "-of=" + NotExistingFilePath, NotExistingFilePath)]
		public void Given_ContextArgument_ContextPropertyIsSet(string propertyName, string argument, object expectedValue)
		{
			Func<ConsoleArgsContext, object> propertySelector = propertyName switch
			{
				nameof(ConsoleArgsContext.IsHelp) => x => x.IsHelp,
				nameof(ConsoleArgsContext.IsSilent) => x => x.IsSilent,
				nameof(ConsoleArgsContext.ResultFile) => x => x.ResultFile,
				nameof(ConsoleArgsContext.SummaryFile) => x => x.SummaryFile,

				_ => throw new ArgumentOutOfRangeException(argument),
			};

			var arguments = new[] { argument };
			var context = ConsoleArgsContext.Parse(arguments);

			Assert.IsFalse(context.HasError);

			var actualValue = propertySelector(context);
			Assert.AreEqual(expectedValue, actualValue);
		}

		[DataTestMethod]
		[DataRow(nameof(UpdaterParameters.SolutionRoot), "--solution=" + NotExistingFilePath, NotExistingFilePath)]
		[DataRow(nameof(UpdaterParameters.SolutionRoot), "-s=" + NotExistingFilePath, NotExistingFilePath)]
		[DataRow(nameof(UpdaterParameters.PackageAuthors), "--packageAuthor=" + SomeText, SomeText)]
		[DataRow(nameof(UpdaterParameters.PackageAuthors), "-a=" + SomeText, SomeText)]
		[DataRow(nameof(UpdaterParameters.IsDowngradeAllowed), "--allowDowngrade", true)]
		[DataRow(nameof(UpdaterParameters.IsDowngradeAllowed), "-d", true)]
		[DataRow(nameof(UpdaterParameters.Strict), "--strict", true)]
		[DataRow(nameof(UpdaterParameters.IsDryRun), "--dryrun", true)]
		public void Given_UpdaterParametersArgument_UpdaterParametersPropertyIsSet(string propertyName, string argument, object expectedValue)
		{
			Func<UpdaterParameters, object> propertySelector = propertyName switch
			{
				nameof(UpdaterParameters.SolutionRoot) => x => x.SolutionRoot,
				nameof(UpdaterParameters.PackageAuthors) => x => x.PackageAuthors,
				nameof(UpdaterParameters.IsDowngradeAllowed) => x => x.IsDowngradeAllowed,
				nameof(UpdaterParameters.Strict) => x => x.Strict,
				nameof(UpdaterParameters.IsDryRun) => x => x.IsDryRun,

				_ => throw new ArgumentOutOfRangeException(propertyName),
			};

			var arguments = new[] { argument };
			var context = ConsoleArgsContext.Parse(arguments);

			Assert.IsFalse(context.HasError);

			var actualValue = propertySelector(context.Parameters);
			Assert.AreEqual(expectedValue, actualValue);
		}

		private static IEnumerable<object[]> CollectionPropertiesTestSetup() => new (Expression<Func<UpdaterParameters, IEnumerable>>, string, object)[]
		{
			( x => x.Feeds, "--useNuGetorg", PackageFeed.NuGetOrg ),
			( x => x.Feeds, "-n", PackageFeed.NuGetOrg ),
			( x => x.Feeds, "--feed=" + SomePublicFeed, PackageFeed.FromString(SomePublicFeed) ),
			( x => x.Feeds, "--feed=" + SomePrivateFeed, PackageFeed.FromString(SomePrivateFeed) ),
			( x => x.Feeds, "-f=" + SomePublicFeed, PackageFeed.FromString(SomePublicFeed) ),
			( x => x.Feeds, "-f=" + SomePrivateFeed, PackageFeed.FromString(SomePrivateFeed) ),
			( x => x.PackagesToUpdate, "--updatePackages=" + SomeText, SomeText ),
			( x => x.PackagesToUpdate, "--update=" + SomeText, SomeText ),
			( x => x.PackagesToUpdate, "-u=" + SomeText, SomeText ),
			( x => x.PackagesToIgnore, "--ignorePackages=" + SomeText, SomeText ),
			( x => x.PackagesToIgnore, "--ignore=" + SomeText, SomeText ),
			( x => x.PackagesToIgnore, "-i=" + SomeText, SomeText ),
			( x => x.TargetVersions, "--versions=" + SomeText, SomeText ),
			( x => x.TargetVersions, "--version=" + SomeText, SomeText ),
			( x => x.TargetVersions, "-v=" + SomeText, SomeText ),
		}.Select(x => new[] { x.Item1, x.Item2, x.Item3 });

		[DataTestMethod]
		[DynamicData(nameof(CollectionPropertiesTestSetup), DynamicDataSourceType.Method)]
		public void Given_UpdaterParametersArgument_ContextCollectionPropertyIsSet(Expression<Func<UpdaterParameters, IEnumerable>> propertySelector, string argument, object expectedValue)
		{
			var arguments = new[] { argument };
			var context = ConsoleArgsContext.Parse(arguments);

			Assert.IsFalse(context.HasError);

			var collection = propertySelector.Compile()(context.Parameters);
			var actualValue = collection?.Cast<object>().FirstOrDefault();
			Assert.AreEqual(expectedValue, actualValue);
		}

		[TestMethod]
		[DeploymentItem(PinnedVersionJsonPath)]
		public void Given_UpdaterParametersArgument_ContextTargetVersionIsSet()
		{
			var arguments = new[] { "--versionOverrides=" + PinnedVersionJsonPath };
			var context = ConsoleArgsContext.Parse(arguments);

			Assert.IsFalse(context.HasError);

			var actualValues = (ICollection)context.Parameters.VersionOverrides;
			var expectedValues = ConsoleArgsContext.LoadOverrides(PinnedVersionJsonPath);

			CollectionAssert.AreEqual(expectedValues, actualValues);
		}
	}
}
