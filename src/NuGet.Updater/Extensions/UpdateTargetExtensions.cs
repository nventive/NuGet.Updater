using NuGet.Updater.Entities;

namespace NuGet.Updater.Extensions
{
	public static class UpdateTargetExtensions
	{
		public static string GetDescription(this UpdateTarget target)
		{
			switch(target)
			{
				case UpdateTarget.Nuspec:
					return ".nuspec";
				case UpdateTarget.ProjectJson:
					return "project.json";
				case UpdateTarget.PackageReference:
					return ".csproj";
				case UpdateTarget.DirectoryProps:
					return "Directory.Build.targets";
				case UpdateTarget.DirectoryTargets:
					return "Directory.Build.props";
				default:
					return default;
			}
		}
	}
}
