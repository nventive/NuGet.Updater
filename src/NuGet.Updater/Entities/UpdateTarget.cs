namespace NuGet.Updater.Entities
{
	/// <summary>
	/// The type of files to update.
	/// </summary>
	public enum UpdateTarget
	{
		/// <summary>
		/// No files will be updated.
		/// </summary>
		Unespecified = 0,

		/// <summary>
		/// .nuspec files.
		/// </summary>
		Nuspec = 2,

		/// <summary>
		/// project.json files.
		/// </summary>
		ProjectJson = 4,

		/// <summary>
		/// PackageReferences from csproj.
		/// </summary>
		PackageReference = 8,

		/// <summary>
		/// Directory.Build.props files.
		/// </summary>
		DirectoryProps = 16,

		/// <summary>
		/// Directory.Build.targets files.
		/// </summary>
		DirectoryTargets = 32,

		/// <summary>
		/// All the supported file types.
		/// </summary>
		All = Nuspec | ProjectJson | PackageReference | DirectoryProps | DirectoryTargets,
	}
}
