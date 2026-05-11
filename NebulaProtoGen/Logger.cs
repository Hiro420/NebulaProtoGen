using ProtoDescDumper.Core.Abstractions;
using System.Drawing;

namespace NebulaProtoGen;

public class Logger(string instanceName) : ILogger
{
	public bool DoLogUselessInfo = true;
	private readonly string _instanceName = instanceName;

	public Logger() : this(typeof(Logger).Namespace ?? "NebulaProtoGen")
	{

	}

	public static void ClearConsole() => Colorful.Console.ResetColor();

	public void Error(string message)
	{
		LogMessage(message, LogColors.ERROR);
	}

	public void Error(string message, Exception ex)
	{
		LogMessage($"{message}: {ex.Message}", LogColors.ERROR);
	}

	public void Warn(string message)
	{
		LogMessage(message, LogColors.WARNING);
	}

	public void Info(string message)
	{
		if (DoLogUselessInfo)
			LogMessage(message, LogColors.INFO);
	}

	public void Success(string message, bool isImportant = true)
	{
		if (DoLogUselessInfo || isImportant)
			LogMessage(message, LogColors.SUCCESS);
	}

	private void LogMessage(string message, LogColors color)
	{
		Color colorCode = ColorTranslator.FromHtml($"#{(int)color:X6}");
		Colorful.Console.WriteLine($"[{_instanceName}][{color}] {message}", colorCode);
		ClearConsole();
	}

	internal enum LogColors
	{
		None = 0xFFFFFF,
		INFO = 0x61AFEF,
		WARNING = 0xE5C07B,
		ERROR = 0xE06C75,
		SUCCESS = 0x98C379
	}
}