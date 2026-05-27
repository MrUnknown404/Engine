namespace Engine3.Core.Utility.Compatability;

public static class Windows {
	// TODO call timeBeginPeriod/timeEndPeriod on windows https://learn.microsoft.com/en-us/windows/win32/api/synchapi/nf-synchapi-sleep

	internal static void Setup() { }
	internal static void Cleanup() { }
}