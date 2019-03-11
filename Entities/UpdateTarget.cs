using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nuget.Updater
{
	/// <summary>
	/// The type of files to update
	/// </summary>
	public enum UpdateTarget
	{
		Nuspec = 2,
		ProjectJson = 4,
		PackageReference = 8,
        DirectoryProps = 16,
        DirectoryTargets = 32,

		All = Nuspec | ProjectJson | PackageReference | DirectoryProps | DirectoryTargets,
    }
}
