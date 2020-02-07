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
			var (context, _) = ConsoleArgsParser.Parse(arguments);

			Assert.IsTrue(context.IsHelp);
		}

		[TestMethod]
		public void Given_InvalidArgument_ContextIsError()
		{
			var arguments = new[] { "--absolutelyWrong" };
			var (context, _) = ConsoleArgsParser.Parse(arguments);

			//throw new NotImplementedException();
			Assert.IsTrue(context.IsHelp);
		}
	}
}
