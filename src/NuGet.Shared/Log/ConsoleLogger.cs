using System;
using System.Threading.Tasks;
using NuGet.Common;

namespace NuGet.Shared.Entities
{
	public class ConsoleLogger : ILogger
	{
		public static readonly ConsoleLogger Instance = new ConsoleLogger();

		private ConsoleLogger()
		{
		}

		public void Log(LogLevel level, string data) => Log(new LogMessage(level, data));

		public void Log(ILogMessage message) => Console.WriteLine(message.Message);

		public async Task LogAsync(LogLevel level, string data) => Log(level, data);

		public async Task LogAsync(ILogMessage message) => Log(message);

		public void LogDebug(string data) => Log(LogLevel.Debug, data);

		public void LogError(string data) => Log(LogLevel.Error, data);

		public void LogInformation(string data) => Log(LogLevel.Information, data);

		public void LogInformationSummary(string data) => Log(LogLevel.Information, data);

		public void LogMinimal(string data) => Log(LogLevel.Minimal, data);

		public void LogVerbose(string data) => Log(LogLevel.Verbose, data);

		public void LogWarning(string data) => Log(LogLevel.Warning, data);
	}
}
