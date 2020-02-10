using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NuGet.Updater.Tool;

namespace NuGet.Updater.Tests
{
	[TestClass]
	public class ConsoleArgsParserTests
	{
		[TestMethod]
		public void Given_HelpArgument_ContextIsHelp()
		{
			var arguments = new[] { "-help" };
			var context = ConsoleArgsParser.Parse(arguments);

			Assert.IsTrue(context.IsHelp);
		}

		[TestMethod]
		public void Given_UnrecognizedArgument_ContextHasError()
		{
			var arguments = new[] { "--absolutelyWrong" };
			var context = ConsoleArgsParser.Parse(arguments);

			Assert.IsTrue(context.HasError);
			Assert.AreEqual(context.Errors[0].Argument, arguments[0]);
			Assert.AreEqual(context.Errors[0].Type, ConsoleArgsParser.ConsoleArgError.ErrorType.UnrecognizedArgument);
		}

		[TestMethod]
		[Ignore("fixme: Mono.Options recognizes `- asd` because it is parsed as parameter `-a` with a value of `sd`")]
		public void Given_UnrecognizedArgument_ContextHasError2()
		{
			var arguments = new[] { "-asd" };
			var context = ConsoleArgsParser.Parse(arguments);

			Assert.IsTrue(context.HasError);
			Assert.AreEqual(context.Errors[0].Argument, arguments[0]);
			Assert.AreEqual(context.Errors[0].Type, ConsoleArgsParser.ConsoleArgError.ErrorType.UnrecognizedArgument);
		}

		[TestMethod]
		public void Given_InvalidArgumentParameter_ContextHasError()
		{
			const string MissingFile = @"c:\not\existing\file.mia";
			var arguments = new[] { $"--versionOverrides={MissingFile}" };
			var context = ConsoleArgsParser.Parse(arguments);

			Assert.IsTrue(context.HasError);
			Assert.AreEqual(context.Errors[0].Argument, MissingFile);
			Assert.AreEqual(context.Errors[0].Type, ConsoleArgsParser.ConsoleArgError.ErrorType.ValueParsingError);
			Assert.IsInstanceOfType(context.Errors[0].Exception, typeof(DirectoryNotFoundException));
		}
	}
}
