using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NuGet.Shared.Entities;
using NuGet.Shared.Helpers;

namespace NuGet.Updater.Tests
{
	[TestClass]
	public class SolutionHelperTests
	{
		[TestMethod]
		public async Task GivenSolution_PackageReferencesAreFound()
		{
			var solution = @"C:\Git\MyMD\MyMD\MyMD.sln";

			var references = await SolutionHelper.GetPackageReferences(CancellationToken.None, solution, FileType.Csproj);

			Assert.IsTrue(references.Any());
		}
	}
}
