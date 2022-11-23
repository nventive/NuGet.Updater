using System;

namespace NvGet.Contracts
{
	/// <summary>
	/// The type of file containing a reference.
	/// </summary>
	[Flags]
	public enum FileType
	{
		/// <summary>
		/// No files will be updated.
		/// </summary>
		Unspecified = 0,

		/// <summary>
		/// .nuspec files.
		/// </summary>
		Nuspec = 2,

		/// <summary>
		/// PackageReferences from csproj.
		/// </summary>
		Csproj = 4,

		/// <summary>
		/// Directory.Build.props files.
		/// </summary>
		DirectoryProps = 8,

		/// <summary>
		/// Directory.Build.targets files.
		/// </summary>
		DirectoryTargets = 16,

		/// <summary>
		/// Directory.Packages.props files.
		/// </summary>
		CentralPackageManagement = 32,

		/// <summary>
		/// All the supported file types.
		/// </summary>
		All = Nuspec | Csproj | DirectoryProps | DirectoryTargets | CentralPackageManagement,
	}
}
