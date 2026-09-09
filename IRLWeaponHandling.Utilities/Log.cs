using BepInEx.Logging;
using IRLWeaponHandling.Configuration;

namespace IRLWeaponHandling.Utilities;

internal static class Log
{
	private static ManualLogSource _source;

	public static void Init(ManualLogSource source)
	{
		_source = source;
	}

	public static void Info(string message)
	{
		_source?.LogInfo(message);
	}

	public static void Warning(string message)
	{
		_source?.LogWarning(message);
	}

	public static void Error(string message)
	{
		_source?.LogError(message);
	}

	public static void Verbose(string message)
	{
		if (HandlingConfig.VerboseLogging != null && HandlingConfig.VerboseLogging.Value)
		{
			_source?.LogInfo(message);
		}
	}
}
