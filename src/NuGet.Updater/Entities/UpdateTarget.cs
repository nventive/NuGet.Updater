using System;

namespace NuGet.Updater.Entities
{
	/// <summary>
	/// The type of files to update.
	/// </summary>
	[Flags]
	public enum UpdateTarget
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
		/// All the supported file types.
		/// </summary>
		All = Nuspec | Csproj | DirectoryProps | DirectoryTargets,
	}
}
