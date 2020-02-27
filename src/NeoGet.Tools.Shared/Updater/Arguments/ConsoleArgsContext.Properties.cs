using System.Collections.Generic;
using System.Linq;
using NeoGet.Tools.Updater.Entities;

namespace NeoGet.Tools.Updater.Arguments
{
	public partial class ConsoleArgsContext
	{
		private ConsoleArgsContext() { }

		public bool HasError => Errors.Any();

		public IList<ConsoleArgError> Errors { get; } = new List<ConsoleArgError>();

		public bool IsHelp { get; set; }

		public bool IsSilent { get; set; }

		public string SummaryFile { get; set; }

		public string ResultFile { get; set; }

		public UpdaterParameters Parameters { get; set; }
	}
}
