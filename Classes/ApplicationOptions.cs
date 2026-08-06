using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Text;
using System.Windows.Media;

namespace DoMping.Classes;

public static class ApplicationOptions
{
	public enum PopupNotificationOption
	{
		Always,
		Never,
		WhenMinimized
	}

	public enum StartMode
	{
		Blank,
		MultiInput,
		Favorite
	}

	public static int PingInterval { get; set; }

	public static int PingTimeout { get; set; }

	public static int AlertThreshold { get; set; }

	public static bool IsEmailAlertEnabled { get; set; }

	public static bool IsEmailAuthenticationRequired { get; set; }

	public static bool IsEmailSslEnabled { get; set; }

	public static bool IsAudioUpAlertEnabled { get; set; }

	public static bool IsAudioDownAlertEnabled { get; set; }

	public static string AudioUpFilePath { get; set; }

	public static string AudioDownFilePath { get; set; }

	public static string EmailServer { get; set; }

	public static string EmailUser { get; set; }

	public static string EmailPassword { get; set; }

	public static string EmailPort { get; set; }

	public static string EmailRecipient { get; set; }

	public static string EmailFromAddress { get; set; }

	public static bool IsLogOutputEnabled { get; set; }

	public static string LogPath { get; set; }

	public static bool IsLogStatusChangesEnabled { get; set; }

	public static string LogStatusChangesPath { get; set; }

	public static bool IsAutoDismissEnabled { get; set; }

	public static int AutoDismissMilliseconds { get; set; }

	public static PopupNotificationOption PopupOption { get; set; }

	public static PingDisplayMode ProbeDisplayMode
	{
		get
		{
			return DisplaySettings.Instance.Mode;
		}
		set
		{
			DisplaySettings.Instance.Mode = value;
		}
	}

	public static string ServiceTcpPorts { get; set; }

	public static string ServiceUdpPorts { get; set; }

	public static int TTL { get; set; }

	public static bool DontFragment { get; set; }

	public static bool UseCustomBuffer { get; set; }

	public static byte[] Buffer { get; set; }

	public static PingOptions GetPingOptions { get; }

	public static StartMode InitialStartMode { get; set; }

	public static int InitialProbeCount { get; set; }

	public static int InitialColumnCount { get; set; }

	public static string InitialFavorite { get; set; }

	public static bool IsAlwaysOnTopEnabled { get; set; }

	public static bool IsMinimizeToTrayEnabled { get; set; }

	public static bool IsExitToTrayEnabled { get; set; }

	public static int FontSize_Probe { get; set; }

	public static int FontSize_Scanner { get; set; }

	public static string BackgroundColor_Probe_Inactive { get; set; }

	public static string BackgroundColor_Probe_Up { get; set; }

	public static string BackgroundColor_Probe_Down { get; set; }

	public static string BackgroundColor_Probe_Indeterminate { get; set; }

	public static string BackgroundColor_Probe_Error { get; set; }

	public static string BackgroundColor_Probe_Scanner { get; set; }

	public static string ForegroundColor_Probe_Inactive { get; set; }

	public static string ForegroundColor_Probe_Up { get; set; }

	public static string ForegroundColor_Probe_Down { get; set; }

	public static string ForegroundColor_Probe_Indeterminate { get; set; }

	public static string ForegroundColor_Probe_Error { get; set; }

	public static string ForegroundColor_Probe_Scanner { get; set; }

	public static string ForegroundColor_Stats_Inactive { get; set; }

	public static string ForegroundColor_Stats_Up { get; set; }

	public static string ForegroundColor_Stats_Down { get; set; }

	public static string ForegroundColor_Stats_Indeterminate { get; set; }

	public static string ForegroundColor_Stats_Error { get; set; }

	public static string ForegroundColor_Alias_Inactive { get; set; }

	public static string ForegroundColor_Alias_Up { get; set; }

	public static string ForegroundColor_Alias_Down { get; set; }

	public static string ForegroundColor_Alias_Indeterminate { get; set; }

	public static string ForegroundColor_Alias_Error { get; set; }

	public static string ForegroundColor_Alias_Scanner { get; set; }

	static ApplicationOptions()
	{
		PingInterval = 2000;
		PingTimeout = 2000;
		AlertThreshold = 2;
		IsEmailAlertEnabled = false;
		IsEmailAuthenticationRequired = false;
		IsEmailSslEnabled = false;
		IsAudioUpAlertEnabled = false;
		IsAudioDownAlertEnabled = false;
		EmailPort = "25";
		IsLogOutputEnabled = false;
		IsLogStatusChangesEnabled = false;
		IsAutoDismissEnabled = false;
		AutoDismissMilliseconds = 7000;
		PopupOption = PopupNotificationOption.Always;
		ServiceTcpPorts = Constants.DefaultServiceTcpPorts;
		ServiceUdpPorts = Constants.DefaultServiceUdpPorts;
		TTL = 64;
		DontFragment = false;
		UseCustomBuffer = false;
		InitialStartMode = StartMode.Blank;
		InitialProbeCount = 2;
		InitialColumnCount = 2;
		InitialFavorite = null;
		IsAlwaysOnTopEnabled = false;
		IsMinimizeToTrayEnabled = false;
		IsExitToTrayEnabled = false;
		FontSize_Probe = 11;
		FontSize_Scanner = 16;
		BackgroundColor_Probe_Inactive = "#3a4a41";
		BackgroundColor_Probe_Up = "#33ff99";
		BackgroundColor_Probe_Down = "#ff5d5d";
		BackgroundColor_Probe_Indeterminate = "#ffcf5c";
		BackgroundColor_Probe_Error = "#ffa94d";
		BackgroundColor_Probe_Scanner = "#7fa89c";
		ForegroundColor_Probe_Inactive = "#5f7d6c";
		ForegroundColor_Probe_Up = "#8dfcc0";
		ForegroundColor_Probe_Down = "#ffb3b3";
		ForegroundColor_Probe_Indeterminate = "#ffe29e";
		ForegroundColor_Probe_Error = "#ffcf9e";
		ForegroundColor_Probe_Scanner = "#b9f1ff";
		ForegroundColor_Stats_Inactive = "#5f7d6c";
		ForegroundColor_Stats_Up = "#33ff99";
		ForegroundColor_Stats_Down = "#ff5d5d";
		ForegroundColor_Stats_Indeterminate = "#ffcf5c";
		ForegroundColor_Stats_Error = "#ffa94d";
		ForegroundColor_Alias_Inactive = "#d3ecdd";
		ForegroundColor_Alias_Up = "#33ff99";
		ForegroundColor_Alias_Down = "#ff5d5d";
		ForegroundColor_Alias_Indeterminate = "#ffcf5c";
		ForegroundColor_Alias_Error = "#ffa94d";
		ForegroundColor_Alias_Scanner = "#b9f1ff";
		Buffer = Encoding.ASCII.GetBytes(Constants.DefaultIcmpData);
		GetPingOptions = new PingOptions(64, dontFragment: true);
	}

	public static void UpdatePingOptions()
	{
		GetPingOptions.Ttl = TTL;
		GetPingOptions.DontFragment = DontFragment;
	}

	public static IEnumerable<Visual> GetChildren(this Visual parent, bool recurse = true)
	{
		if (parent == null)
		{
			yield break;
		}
		int count = VisualTreeHelper.GetChildrenCount(parent);
		for (int i = 0; i < count; i++)
		{
			if (!(VisualTreeHelper.GetChild(parent, i) is Visual child))
			{
				continue;
			}
			yield return child;
			if (!recurse)
			{
				continue;
			}
			foreach (Visual child2 in child.GetChildren())
			{
				yield return child2;
			}
		}
	}
}
