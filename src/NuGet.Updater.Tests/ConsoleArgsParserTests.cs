using System;
using System.Collections.Generic;
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
	}
}
