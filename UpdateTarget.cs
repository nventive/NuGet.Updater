using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nuget.Updater
{
	public enum UpdateTarget
	{
		Nuspec = 2,
		ProjectJson = 4,
		PackageReference = 8,

		All = Nuspec | ProjectJson | PackageReference
	}
}
