using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NuGet.Updater.Entities;
using NuGet.Updater.Tool.Arguments;

namespace NuGet.Updater.Tests
{
	[TestClass]
	public class ConsoleArgsParserTests
	{
		private const string NotExistingFilePath = @"c:\not\existing\file.mia";
		private const string SomeText = nameof(SomeText);

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
		[DataRow(nameof(UpdaterParameters.PackageAuthor), "--packageAuthor=" + SomeText, SomeText)]
		[DataRow(nameof(UpdaterParameters.PackageAuthor), "-a=" + SomeText, SomeText)]
		[DataRow(nameof(UpdaterParameters.IsDowngradeAllowed), "--allowDowngrade", true)]
		[DataRow(nameof(UpdaterParameters.IsDowngradeAllowed), "-d", true)]
		[DataRow(nameof(UpdaterParameters.Strict), "--strict", true)]
		[DataRow(nameof(UpdaterParameters.IsDryRun), "--dryrun", true)]
		public void Given_UpdaterParametersArgument_UpdaterParametersPropertyIsSet(string propertyName, string argument, object expectedValue)
		{
			Func<UpdaterParameters, object> propertySelector = propertyName switch
			{
				nameof(UpdaterParameters.SolutionRoot) => x => x.SolutionRoot,
				nameof(UpdaterParameters.PackageAuthor) => x => x.PackageAuthor,
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
	}
}
