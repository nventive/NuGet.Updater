using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NuGet.Updater.Entities;
using NuGet.Updater.Helpers;

namespace NuGet.Updater.Tests
{
	[TestClass]
	public class SolutionHelperTests
	{
		[TestMethod]
		public async Task GivenSolution_PackageReferencesAreFound()
		{
			var solution = @"C:\Git\MyMD\MyMD\MyMD.sln";

			var references = await SolutionHelper.GetPackageReferences(CancellationToken.None, solution, UpdateTarget.Csproj);

			Assert.IsTrue(references.Any());
		}
	}
}
