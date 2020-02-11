using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NuGet.Updater.Tool.Arguments;

namespace NuGet.Updater.Tests
{
	[TestClass]
	public class ConsoleArgsParserTests
	{
		private const string NotExistingFilePath = @"c:\not\existing\file.mia";

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
	}
}
