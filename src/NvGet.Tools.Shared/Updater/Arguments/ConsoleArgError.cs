using System;
using System.Collections.Generic;
using System.Text;

namespace NeoGet.Tools.Updater.Arguments
{
	public class ConsoleArgError
	{
		public ConsoleArgErrorType Type { get; set; }

		public string Argument { get; set; }

		public Exception Exception { get; set; }

		public ConsoleArgError(string argument, ConsoleArgErrorType type, Exception e = null)
		{
			Argument = argument;
			Type = type;
			Exception = e;
		}

		public string Message => Type switch
		{
			ConsoleArgErrorType.UnrecognizedArgument => "unrecognized argument: " + Argument,
			ConsoleArgErrorType.ValueAssignmentError => "error while trying to assign value: " + Argument,
			ConsoleArgErrorType.ValueParsingError => "error while trying to parse value: " + Argument,

			_ => $"{Type}: " + Argument,
		};
	}
}
