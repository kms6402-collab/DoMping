using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Xml;
using System.Xml.Linq;
using DoMping.Properties;
using DoMping.Views;

namespace DoMping.Classes;

internal class Configuration
{
	public static string FilePath;

	static Configuration()
	{
		FilePath = Environment.ExpandEnvironmentVariables("%LOCALAPPDATA%\\DoMping\\DoMping.xml");
		if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + "DoMping.xml"))
		{
			FilePath = AppDomain.CurrentDomain.BaseDirectory + "DoMping.xml";
		}
	}

	public static bool Exists()
	{
		return File.Exists(FilePath);
	}

	public static bool IsReady()
	{
		if (!File.Exists(FilePath))
		{
			NewConfigurationWindow newConfigurationWindow = new NewConfigurationWindow();
			WindowCollection ownedWindows = Application.Current.MainWindow.OwnedWindows;
			if (ownedWindows.Count > 0)
			{
				newConfigurationWindow.Owner = ((ownedWindows[0].OwnedWindows.Count > 0) ? ownedWindows[0].OwnedWindows[0] : ownedWindows[0]);
			}
			else
			{
				newConfigurationWindow.Owner = Application.Current.MainWindow;
			}
			if (newConfigurationWindow.ShowDialog() == false)
			{
				return false;
			}
			if (!Directory.Exists(Path.GetDirectoryName(FilePath)))
			{
				try
				{
					Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
				}
				catch (Exception ex)
				{
					Util.ShowError(Strings.Error_CreateDirectory + " " + ex.Message);
					return false;
				}
			}
			try
			{
				new XDocument(new XElement("domping", new XElement("aliases"), new XElement("favorites"), new XElement("configuration"), new XElement("colors"))).Save(FilePath);
			}
			catch (Exception ex2)
			{
				Util.ShowError(Strings.Error_CreateConfig + " " + ex2.Message);
				return false;
			}
		}
		return true;
	}

	public static string GetEscapedXpath(string xpath)
	{
		if (!xpath.Contains("'"))
		{
			return "'" + xpath + "'";
		}
		if (!xpath.Contains("\""))
		{
			return "\"" + xpath + "\"";
		}
		return "concat('" + xpath.Replace("'", "',\"'\",'") + "')";
	}

	public static void WriteConfigurationOptions()
	{
		if (!IsReady())
		{
			return;
		}
		try
		{
			XDocument xDocument = XDocument.Load(FilePath);
			XElement xElement = xDocument.Element("domping");
			XElement content = GenerateConfigurationNode();
			XElement content2 = GenerateColorsNode();
			xElement.Descendants("configuration").Remove();
			xElement.Descendants("colors").Remove();
			xElement.Add(content);
			xElement.Add(content2);
			xDocument.Save(FilePath);
		}
		catch (Exception ex)
		{
			Util.ShowError(Strings.Error_WriteConfig + " " + ex.Message);
		}
	}

	private static XElement GenerateConfigurationNode()
	{
		XElement xElement = new XElement("configuration");
		xElement.Add(new XElement("option", new XAttribute("name", "PingInterval"), ApplicationOptions.PingInterval), new XElement("option", new XAttribute("name", "PingTimeout"), ApplicationOptions.PingTimeout), new XElement("option", new XAttribute("name", "TTL"), ApplicationOptions.TTL), new XElement("option", new XAttribute("name", "DontFragment"), ApplicationOptions.DontFragment), new XElement("option", new XAttribute("name", "UseCustomBuffer"), ApplicationOptions.UseCustomBuffer), new XElement("option", new XAttribute("name", "Buffer"), Encoding.ASCII.GetString(ApplicationOptions.Buffer)), new XElement("option", new XAttribute("name", "AlertThreshold"), ApplicationOptions.AlertThreshold), new XElement("option", new XAttribute("name", "ServiceTcpPorts"), ApplicationOptions.ServiceTcpPorts ?? string.Empty), new XElement("option", new XAttribute("name", "ServiceUdpPorts"), ApplicationOptions.ServiceUdpPorts ?? string.Empty), new XComment(" [InitialStartMode] Blank, MultiInput, Favorite "), new XElement("option", new XAttribute("name", "InitialStartMode"), ApplicationOptions.InitialStartMode), new XElement("option", new XAttribute("name", "InitialProbeCount"), ApplicationOptions.InitialProbeCount), new XElement("option", new XAttribute("name", "InitialColumnCount"), ApplicationOptions.InitialColumnCount), new XElement("option", new XAttribute("name", "InitialFavorite"), ApplicationOptions.InitialFavorite ?? string.Empty), new XComment(" [PopupNotifications] Always, Never, WhenMinimized "), new XElement("option", new XAttribute("name", "PopupNotifications"), ApplicationOptions.PopupOption), new XElement("option", new XAttribute("name", "IsAutoDismissEnabled"), ApplicationOptions.IsAutoDismissEnabled), new XElement("option", new XAttribute("name", "AutoDismissMilliseconds"), ApplicationOptions.AutoDismissMilliseconds), new XElement("option", new XAttribute("name", "IsEmailAlertEnabled"), ApplicationOptions.IsEmailAlertEnabled), new XElement("option", new XAttribute("name", "EmailServer"), ApplicationOptions.EmailServer ?? string.Empty), new XElement("option", new XAttribute("name", "EmailPort"), ApplicationOptions.EmailPort ?? string.Empty), new XElement("option", new XAttribute("name", "IsEmailSslEnabled"), ApplicationOptions.IsEmailSslEnabled), new XElement("option", new XAttribute("name", "IsEmailAuthenticationRequired"), ApplicationOptions.IsEmailAuthenticationRequired), new XElement("option", new XAttribute("name", "EmailUser"), string.IsNullOrWhiteSpace(ApplicationOptions.EmailUser) ? string.Empty : Util.EncryptStringAES(ApplicationOptions.EmailUser)), new XElement("option", new XAttribute("name", "EmailPassword"), string.IsNullOrWhiteSpace(ApplicationOptions.EmailPassword) ? string.Empty : Util.EncryptStringAES(ApplicationOptions.EmailPassword)), new XElement("option", new XAttribute("name", "EmailRecipient"), ApplicationOptions.EmailRecipient ?? string.Empty), new XElement("option", new XAttribute("name", "EmailFromAddress"), ApplicationOptions.EmailFromAddress ?? string.Empty), new XElement("option", new XAttribute("name", "IsAudioUpAlertEnabled"), ApplicationOptions.IsAudioUpAlertEnabled), new XElement("option", new XAttribute("name", "AudioUpFilePath"), ApplicationOptions.AudioUpFilePath ?? string.Empty), new XElement("option", new XAttribute("name", "IsAudioDownAlertEnabled"), ApplicationOptions.IsAudioDownAlertEnabled), new XElement("option", new XAttribute("name", "AudioDownFilePath"), ApplicationOptions.AudioDownFilePath ?? string.Empty), new XElement("option", new XAttribute("name", "IsLogOutputEnabled"), ApplicationOptions.IsLogOutputEnabled), new XElement("option", new XAttribute("name", "LogPath"), ApplicationOptions.LogPath ?? string.Empty), new XElement("option", new XAttribute("name", "IsLogStatusChangesEnabled"), ApplicationOptions.IsLogStatusChangesEnabled), new XElement("option", new XAttribute("name", "LogStatusChangesPath"), ApplicationOptions.LogStatusChangesPath ?? string.Empty), new XElement("option", new XAttribute("name", "IsAlwaysOnTopEnabled"), ApplicationOptions.IsAlwaysOnTopEnabled), new XElement("option", new XAttribute("name", "IsMinimizeToTrayEnabled"), ApplicationOptions.IsMinimizeToTrayEnabled), new XElement("option", new XAttribute("name", "IsExitToTrayEnabled"), ApplicationOptions.IsExitToTrayEnabled), new XComment(" [ProbeDisplayMode] Log, Graph, Both "), new XElement("option", new XAttribute("name", "ProbeDisplayMode"), ApplicationOptions.ProbeDisplayMode));
		return xElement;
	}

	private static XElement GenerateColorsNode()
	{
		XElement xElement = new XElement("colors");
		xElement.Add(new XComment(" Probe background "), new XElement("option", new XAttribute("name", "Probe.Background.Inactive"), ApplicationOptions.BackgroundColor_Probe_Inactive), new XElement("option", new XAttribute("name", "Probe.Background.Up"), ApplicationOptions.BackgroundColor_Probe_Up), new XElement("option", new XAttribute("name", "Probe.Background.Down"), ApplicationOptions.BackgroundColor_Probe_Down), new XElement("option", new XAttribute("name", "Probe.Background.Indeterminate"), ApplicationOptions.BackgroundColor_Probe_Indeterminate), new XElement("option", new XAttribute("name", "Probe.Background.Error"), ApplicationOptions.BackgroundColor_Probe_Error), new XComment(" Probe foreground "), new XElement("option", new XAttribute("name", "Probe.Foreground.Inactive"), ApplicationOptions.ForegroundColor_Probe_Inactive), new XElement("option", new XAttribute("name", "Probe.Foreground.Up"), ApplicationOptions.ForegroundColor_Probe_Up), new XElement("option", new XAttribute("name", "Probe.Foreground.Down"), ApplicationOptions.ForegroundColor_Probe_Down), new XElement("option", new XAttribute("name", "Probe.Foreground.Indeterminate"), ApplicationOptions.ForegroundColor_Probe_Indeterminate), new XElement("option", new XAttribute("name", "Probe.Foreground.Error"), ApplicationOptions.ForegroundColor_Probe_Error), new XComment(" Statistics foreground "), new XElement("option", new XAttribute("name", "Statistics.Foreground.Inactive"), ApplicationOptions.ForegroundColor_Stats_Inactive), new XElement("option", new XAttribute("name", "Statistics.Foreground.Up"), ApplicationOptions.ForegroundColor_Stats_Up), new XElement("option", new XAttribute("name", "Statistics.Foreground.Down"), ApplicationOptions.ForegroundColor_Stats_Down), new XElement("option", new XAttribute("name", "Statistics.Foreground.Indeterminate"), ApplicationOptions.ForegroundColor_Stats_Indeterminate), new XElement("option", new XAttribute("name", "Statistics.Foreground.Error"), ApplicationOptions.ForegroundColor_Stats_Error), new XComment(" Alias foreground "), new XElement("option", new XAttribute("name", "Alias.Foreground.Inactive"), ApplicationOptions.ForegroundColor_Alias_Inactive), new XElement("option", new XAttribute("name", "Alias.Foreground.Up"), ApplicationOptions.ForegroundColor_Alias_Up), new XElement("option", new XAttribute("name", "Alias.Foreground.Down"), ApplicationOptions.ForegroundColor_Alias_Down), new XElement("option", new XAttribute("name", "Alias.Foreground.Indeterminate"), ApplicationOptions.ForegroundColor_Alias_Indeterminate), new XElement("option", new XAttribute("name", "Alias.Foreground.Error"), ApplicationOptions.ForegroundColor_Alias_Error));
		return xElement;
	}

	public static void Load()
	{
		if (!Exists())
		{
			return;
		}
		try
		{
			XmlDocument xmlDocument = new XmlDocument();
			xmlDocument.Load(FilePath);
			LoadConfigurationNode(xmlDocument.SelectNodes("/domping/configuration/option"));
			LoadColorsNode(xmlDocument.SelectNodes("/domping/colors/option"));
			ApplicationOptions.UpdatePingOptions();
		}
		catch (Exception ex)
		{
			Util.ShowError(Strings.Error_LoadConfig + " " + ex.Message);
		}
	}

	private static void LoadColorsNode(XmlNodeList nodeList)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		foreach (XmlNode node in nodeList)
		{
			dictionary.Add(node.Attributes["name"].Value, node.InnerText);
		}
		if (dictionary.TryGetValue("Probe.Background.Inactive", out var value) && Util.IsValidHtmlColor(value))
		{
			ApplicationOptions.BackgroundColor_Probe_Inactive = value;
		}
		if (dictionary.TryGetValue("Probe.Background.Up", out value) && Util.IsValidHtmlColor(value))
		{
			ApplicationOptions.BackgroundColor_Probe_Up = value;
		}
		if (dictionary.TryGetValue("Probe.Background.Down", out value) && Util.IsValidHtmlColor(value))
		{
			ApplicationOptions.BackgroundColor_Probe_Down = value;
		}
		if (dictionary.TryGetValue("Probe.Background.Indeterminate", out value) && Util.IsValidHtmlColor(value))
		{
			ApplicationOptions.BackgroundColor_Probe_Indeterminate = value;
		}
		if (dictionary.TryGetValue("Probe.Background.Error", out value) && Util.IsValidHtmlColor(value))
		{
			ApplicationOptions.BackgroundColor_Probe_Error = value;
		}
		if (dictionary.TryGetValue("Probe.Foreground.Inactive", out value) && Util.IsValidHtmlColor(value))
		{
			ApplicationOptions.ForegroundColor_Probe_Inactive = value;
		}
		if (dictionary.TryGetValue("Probe.Foreground.Up", out value) && Util.IsValidHtmlColor(value))
		{
			ApplicationOptions.ForegroundColor_Probe_Up = value;
		}
		if (dictionary.TryGetValue("Probe.Foreground.Down", out value) && Util.IsValidHtmlColor(value))
		{
			ApplicationOptions.ForegroundColor_Probe_Down = value;
		}
		if (dictionary.TryGetValue("Probe.Foreground.Indeterminate", out value) && Util.IsValidHtmlColor(value))
		{
			ApplicationOptions.ForegroundColor_Probe_Indeterminate = value;
		}
		if (dictionary.TryGetValue("Probe.Foreground.Error", out value) && Util.IsValidHtmlColor(value))
		{
			ApplicationOptions.ForegroundColor_Probe_Error = value;
		}
		if (dictionary.TryGetValue("Statistics.Foreground.Inactive", out value) && Util.IsValidHtmlColor(value))
		{
			ApplicationOptions.ForegroundColor_Stats_Inactive = value;
		}
		if (dictionary.TryGetValue("Statistics.Foreground.Up", out value) && Util.IsValidHtmlColor(value))
		{
			ApplicationOptions.ForegroundColor_Stats_Up = value;
		}
		if (dictionary.TryGetValue("Statistics.Foreground.Down", out value) && Util.IsValidHtmlColor(value))
		{
			ApplicationOptions.ForegroundColor_Stats_Down = value;
		}
		if (dictionary.TryGetValue("Statistics.Foreground.Indeterminate", out value) && Util.IsValidHtmlColor(value))
		{
			ApplicationOptions.ForegroundColor_Stats_Indeterminate = value;
		}
		if (dictionary.TryGetValue("Statistics.Foreground.Error", out value) && Util.IsValidHtmlColor(value))
		{
			ApplicationOptions.ForegroundColor_Stats_Error = value;
		}
		if (dictionary.TryGetValue("Alias.Foreground.Inactive", out value) && Util.IsValidHtmlColor(value))
		{
			ApplicationOptions.ForegroundColor_Alias_Inactive = value;
		}
		if (dictionary.TryGetValue("Alias.Foreground.Up", out value) && Util.IsValidHtmlColor(value))
		{
			ApplicationOptions.ForegroundColor_Alias_Up = value;
		}
		if (dictionary.TryGetValue("Alias.Foreground.Down", out value) && Util.IsValidHtmlColor(value))
		{
			ApplicationOptions.ForegroundColor_Alias_Down = value;
		}
		if (dictionary.TryGetValue("Alias.Foreground.Indeterminate", out value) && Util.IsValidHtmlColor(value))
		{
			ApplicationOptions.ForegroundColor_Alias_Indeterminate = value;
		}
		if (dictionary.TryGetValue("Alias.Foreground.Error", out value) && Util.IsValidHtmlColor(value))
		{
			ApplicationOptions.ForegroundColor_Alias_Error = value;
		}
	}

	private static void LoadConfigurationNode(XmlNodeList nodeList)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		foreach (XmlNode node in nodeList)
		{
			dictionary.Add(node.Attributes["name"].Value, node.InnerText);
		}
		if (dictionary.TryGetValue("PingInterval", out var value))
		{
			ApplicationOptions.PingInterval = int.Parse(value);
		}
		if (dictionary.TryGetValue("PingTimeout", out value))
		{
			ApplicationOptions.PingTimeout = int.Parse(value);
		}
		if (dictionary.TryGetValue("TTL", out value))
		{
			ApplicationOptions.TTL = int.Parse(value);
		}
		if (dictionary.TryGetValue("DontFragment", out value))
		{
			ApplicationOptions.DontFragment = bool.Parse(value);
		}
		if (dictionary.TryGetValue("UseCustomBuffer", out value))
		{
			ApplicationOptions.UseCustomBuffer = bool.Parse(value);
		}
		if (dictionary.TryGetValue("Buffer", out value))
		{
			ApplicationOptions.Buffer = Encoding.ASCII.GetBytes(value);
		}
		if (dictionary.TryGetValue("AlertThreshold", out value))
		{
			ApplicationOptions.AlertThreshold = int.Parse(value);
		}
		if (dictionary.TryGetValue("ServiceTcpPorts", out value))
		{
			ApplicationOptions.ServiceTcpPorts = value;
		}
		if (dictionary.TryGetValue("ServiceUdpPorts", out value))
		{
			ApplicationOptions.ServiceUdpPorts = value;
		}
		if (dictionary.TryGetValue("InitialStartMode", out value))
		{
			if (value.Equals(ApplicationOptions.StartMode.Favorite.ToString()))
			{
				ApplicationOptions.InitialStartMode = ApplicationOptions.StartMode.Favorite;
			}
			else if (value.Equals(ApplicationOptions.StartMode.MultiInput.ToString()))
			{
				ApplicationOptions.InitialStartMode = ApplicationOptions.StartMode.MultiInput;
			}
			else
			{
				ApplicationOptions.InitialStartMode = ApplicationOptions.StartMode.Blank;
			}
		}
		if (dictionary.TryGetValue("InitialProbeCount", out value))
		{
			ApplicationOptions.InitialProbeCount = int.Parse(value);
		}
		if (dictionary.TryGetValue("InitialColumnCount", out value))
		{
			ApplicationOptions.InitialColumnCount = int.Parse(value);
		}
		if (dictionary.TryGetValue("InitialFavorite", out value))
		{
			ApplicationOptions.InitialFavorite = value;
		}
		if (dictionary.TryGetValue("PopupNotifications", out value))
		{
			if (value.Equals(ApplicationOptions.PopupNotificationOption.Always.ToString()))
			{
				ApplicationOptions.PopupOption = ApplicationOptions.PopupNotificationOption.Always;
			}
			else if (value.Equals(ApplicationOptions.PopupNotificationOption.WhenMinimized.ToString()))
			{
				ApplicationOptions.PopupOption = ApplicationOptions.PopupNotificationOption.WhenMinimized;
			}
			else
			{
				ApplicationOptions.PopupOption = ApplicationOptions.PopupNotificationOption.Never;
			}
		}
		if (dictionary.TryGetValue("IsAutoDismissEnabled", out value))
		{
			ApplicationOptions.IsAutoDismissEnabled = bool.Parse(value);
		}
		if (dictionary.TryGetValue("AutoDismissMilliseconds", out value))
		{
			ApplicationOptions.AutoDismissMilliseconds = int.Parse(value);
		}
		if (dictionary.TryGetValue("IsEmailAlertEnabled", out value))
		{
			ApplicationOptions.IsEmailAlertEnabled = bool.Parse(value);
		}
		if (dictionary.TryGetValue("IsEmailAuthenticationRequired", out value))
		{
			ApplicationOptions.IsEmailAuthenticationRequired = bool.Parse(value);
		}
		if (dictionary.TryGetValue("EmailServer", out value))
		{
			ApplicationOptions.EmailServer = value;
		}
		if (dictionary.TryGetValue("EmailPort", out value))
		{
			ApplicationOptions.EmailPort = value;
		}
		if (dictionary.TryGetValue("IsEmailSslEnabled", out value))
		{
			ApplicationOptions.IsEmailSslEnabled = bool.Parse(value);
		}
		if (dictionary.TryGetValue("EmailRecipient", out value))
		{
			ApplicationOptions.EmailRecipient = value;
		}
		if (dictionary.TryGetValue("EmailFromAddress", out value))
		{
			ApplicationOptions.EmailFromAddress = value;
		}
		if (dictionary.TryGetValue("IsAudioAlertEnabled", out value))
		{
			ApplicationOptions.IsAudioDownAlertEnabled = bool.Parse(value);
		}
		if (dictionary.TryGetValue("IsAudioUpAlertEnabled", out value))
		{
			ApplicationOptions.IsAudioUpAlertEnabled = bool.Parse(value);
		}
		if (dictionary.TryGetValue("IsAudioDownAlertEnabled", out value))
		{
			ApplicationOptions.IsAudioDownAlertEnabled = bool.Parse(value);
		}
		if (dictionary.TryGetValue("AudioFilePath", out value))
		{
			ApplicationOptions.AudioDownFilePath = value;
		}
		if (dictionary.TryGetValue("AudioUpFilePath", out value))
		{
			ApplicationOptions.AudioUpFilePath = value;
		}
		if (dictionary.TryGetValue("AudioDownFilePath", out value))
		{
			ApplicationOptions.AudioDownFilePath = value;
		}
		if (dictionary.TryGetValue("IsLogOutputEnabled", out value))
		{
			ApplicationOptions.IsLogOutputEnabled = bool.Parse(value);
		}
		if (dictionary.TryGetValue("LogPath", out value))
		{
			ApplicationOptions.LogPath = value;
		}
		if (dictionary.TryGetValue("IsLogStatusChangesEnabled", out value))
		{
			ApplicationOptions.IsLogStatusChangesEnabled = bool.Parse(value);
		}
		if (dictionary.TryGetValue("LogStatusChangesPath", out value))
		{
			ApplicationOptions.LogStatusChangesPath = value;
		}
		if (dictionary.TryGetValue("EmailUser", out value) && value.Length > 0)
		{
			ApplicationOptions.EmailUser = Util.DecryptStringAES(value);
		}
		if (dictionary.TryGetValue("EmailPassword", out value) && value.Length > 0)
		{
			ApplicationOptions.EmailPassword = Util.DecryptStringAES(value);
		}
		if (dictionary.TryGetValue("IsAlwaysOnTopEnabled", out value))
		{
			ApplicationOptions.IsAlwaysOnTopEnabled = bool.Parse(value);
		}
		if (dictionary.TryGetValue("IsMinimizeToTrayEnabled", out value))
		{
			ApplicationOptions.IsMinimizeToTrayEnabled = bool.Parse(value);
		}
		if (dictionary.TryGetValue("IsExitToTrayEnabled", out value))
		{
			ApplicationOptions.IsExitToTrayEnabled = bool.Parse(value);
		}
		if (dictionary.TryGetValue("ProbeDisplayMode", out value))
		{
			if (value.Equals(PingDisplayMode.Log.ToString()))
			{
				ApplicationOptions.ProbeDisplayMode = PingDisplayMode.Log;
			}
			else if (value.Equals(PingDisplayMode.Graph.ToString()))
			{
				ApplicationOptions.ProbeDisplayMode = PingDisplayMode.Graph;
			}
			else
			{
				ApplicationOptions.ProbeDisplayMode = PingDisplayMode.Both;
			}
		}
	}
}
