using System.Windows.Input;

namespace DoMping.Classes;

internal class Constants
{
	public const string Color_Probe_Background_Inactive = "#3a4a41";

	public const string Color_Probe_Background_Up = "#33ff99";

	public const string Color_Probe_Background_Down = "#ff5d5d";

	public const string Color_Probe_Background_Indeterminate = "#ffcf5c";

	public const string Color_Probe_Background_Error = "#ffa94d";

	public const string Color_Probe_Background_Scanner = "#5fd6ff";

	public const string Color_Probe_Foreground_Inactive = "#5f7d6c";

	public const string Color_Probe_Foreground_Up = "#8dfcc0";

	public const string Color_Probe_Foreground_Down = "#ffb3b3";

	public const string Color_Probe_Foreground_Indeterminate = "#ffe29e";

	public const string Color_Probe_Foreground_Error = "#ffcf9e";

	public const string Color_Probe_Foreground_Scanner = "#b9f1ff";

	public const string Color_Statistics_Foreground_Inactive = "#5f7d6c";

	public const string Color_Statistics_Foreground_Up = "#33ff99";

	public const string Color_Statistics_Foreground_Down = "#ff5d5d";

	public const string Color_Statistics_Foreground_Indeterminate = "#ffcf5c";

	public const string Color_Statistics_Foreground_Error = "#ffa94d";

	public const string Color_Alias_Foreground_Inactive = "#d3ecdd";

	public const string Color_Alias_Foreground_Up = "#33ff99";

	public const string Color_Alias_Foreground_Down = "#ff5d5d";

	public const string Color_Alias_Foreground_Indeterminate = "#ffcf5c";

	public const string Color_Alias_Foreground_Error = "#ffa94d";

	public const string Color_Alias_Foreground_Scanner = "#b9f1ff";

	public const string DefaultIcmpData = "DoMpingDoMpingDoMpingDoMpingDoMpingDoMpingDoMping";

	public const string DefaultServiceTcpPorts = "22,23,80,56";

	public const string DefaultServiceUdpPorts = "6";

	public const int DefaultTimeout = 2000;

	public const int DefaultTTL = 64;

	public const int DefaultInterval = 2000;

	public const string DefaultAudioDownFilePath = "%WINDIR%\\Media\\Windows Notify Email.wav";

	public const string DefaultAudioUpFilePath = "%WINDIR%\\Media\\Windows Unlock.wav";

	public const Key StatusHistoryKeyBinding = Key.F12;

	public const Key HelpKeyBinding = Key.F1;
}
