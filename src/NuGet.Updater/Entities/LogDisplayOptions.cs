namespace NuGet.Updater.Entities
{
	internal struct LogDisplayOptions
	{
		public static readonly LogDisplayOptions Summary = new LogDisplayOptions
		{
			IncludeUrls = true,
			PrettifyTable = false,
		};

		public static readonly LogDisplayOptions Console = new LogDisplayOptions
		{
			IncludeUrls = false,
			PrettifyTable = true,
		};

		public bool IncludeUrls { get; set; }

		public bool PrettifyTable { get; set; }
	}
}
