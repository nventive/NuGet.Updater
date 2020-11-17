using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NvGet.Entities;
using NuGet.Frameworks;
using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace NvGet.Contracts
{
	public interface IPackageFeed
	{
		/// <summary>
		/// Gets the URL of the feed.
		/// </summary>
		Uri Url { get; }

		/// <summary>
		/// Gets a value indicating whether the feed is private.
		/// </summary>
		bool IsPrivate { get; }

		/// <summary>
		/// Get available versions for the given package reference.
		/// </summary>
		/// <param name="ct"></param>
		/// <param name="reference"></param>
		/// <param name="author"></param>
		/// <returns></returns>
		Task<NuGetVersion[]> GetPackageVersions(CancellationToken ct, PackageReference reference, string author = null);

		/// <summary>
		/// Get the dependencies of the given package identity.
		/// </summary>
		/// <param name="ct"></param>
		/// <param name="packageIdentity"></param>
		/// <returns></returns>
		Task<Dictionary<NuGetFramework, PackageDependency[]>> GetDependencies(CancellationToken ct, PackageIdentity packageIdentity);

		/// <summary>
		/// Downloads the package with the given identity.
		/// </summary>
		/// <param name="ct"></param>
		/// <param name="packageIdentity"></param>
		/// <param name="location"></param>
		/// <returns></returns>
		Task<LocalPackage> DownloadPackage(CancellationToken ct, PackageIdentity packageIdentity, string location);

		/// <summary>
		/// Pushes the given local package.
		/// </summary>
		/// <param name="ct"></param>
		/// <param name="package"></param>
		/// <returns>A value indicating whether or not the package has been pushed</returns>
		Task<bool> PushPackage(CancellationToken ct, LocalPackage package);
	}
}
