using System;
using System.Collections.Generic;
using System.Text;

namespace NuGet.Updater.Entities
{
	public class PackageReference
	{
		public string Id { get; set; }

		public string Version { get; set; }
		
		public string[] Files { get; set; }

		public override string ToString() => $"{Id} {Version}";
	}
}
