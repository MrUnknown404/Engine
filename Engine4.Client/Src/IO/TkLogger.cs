using OpenTK.Core.Utility;

namespace Engine4.Client.IO;

public class TkLogger : ILogger {
	public LogLevel Filter { get; set; }

	public void LogInternal(string str, LogLevel level, string filePath, int lineNumber, string member) {
		if (level >= LogLevel.Warning) { Console.WriteLine($"[OpenTK] {str}"); } // TODO log properly
	}

	public void Flush() => Console.WriteLine();
}