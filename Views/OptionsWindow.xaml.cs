using System;
using System.IO;
using System.Media;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using DoMping.Classes;

namespace DoMping.Views;

public partial class OptionsWindow : Window, IComponentConnector, IStyleConnector
{
	private const int GWL_STYLE = -16;

	private const int WS_MAXIMIZEBOX = 65536;

	private const int WS_MINIMIZEBOX = 131072;

	[DllImport("user32.dll")]
	private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

	[DllImport("user32.dll")]
	private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

	public OptionsWindow()
	{
		InitializeComponent();
		PopulateGeneralOptions();
		PopulateNotificationOptions();
		PopulateEmailAlertOptions();
		PopulateAudioAlertOptions();
		PopulateLogOutputOptions();
		PopulateAdvancedOptions();
		PopulateServicePortsOptions();
		PopulateDisplayOptions();
		PopulateLayoutOptions();
	}

	private bool? ShowError(string message, TabItem tabItem, System.Windows.Controls.Control control, bool isWarning = false)
	{
		tabItem?.Focus();
		DialogWindow dialogWindow = ((!isWarning) ? DialogWindow.ErrorWindow(message) : DialogWindow.WarningWindow(message, "Save"));
		dialogWindow.Owner = this;
		bool? result = dialogWindow.ShowDialog();
		control?.Focus();
		return result;
	}

	private void PopulateGeneralOptions()
	{
		int pingInterval = ApplicationOptions.PingInterval;
		int pingTimeout = ApplicationOptions.PingTimeout;
		string text;
		int num;
		if (ApplicationOptions.PingInterval >= 3600000 && ApplicationOptions.PingInterval % 3600000 == 0)
		{
			text = "hours";
			num = 3600000;
		}
		else if (ApplicationOptions.PingInterval >= 60000 && ApplicationOptions.PingInterval % 60000 == 0)
		{
			text = "minutes";
			num = 60000;
		}
		else
		{
			text = "seconds";
			num = 1000;
		}
		pingInterval /= num;
		pingTimeout /= 1000;
		PingInterval.Text = pingInterval.ToString();
		PingTimeout.Text = pingTimeout.ToString();
		AlertThreshold.Text = ApplicationOptions.AlertThreshold.ToString();
		PingIntervalUnits.Text = text;
		InitialProbeCount.Text = ApplicationOptions.InitialProbeCount.ToString();
		InitialColumnCount.Text = ApplicationOptions.InitialColumnCount.ToString();
		StartupMode.SelectedIndex = (int)ApplicationOptions.InitialStartMode;
		InitialFavorite.ItemsSource = Favorite.GetTitles();
		InitialFavorite.Text = ((ApplicationOptions.InitialFavorite == null) ? string.Empty : ApplicationOptions.InitialFavorite);
	}

	private void PopulateNotificationOptions()
	{
		PopupsDisabledOption.IsChecked = false;
		PopupsMinimizedOption.IsChecked = false;
		PopupsAlwaysOption.IsChecked = false;
		switch (ApplicationOptions.PopupOption)
		{
		case ApplicationOptions.PopupNotificationOption.Never:
			PopupsDisabledOption.IsChecked = true;
			break;
		case ApplicationOptions.PopupNotificationOption.WhenMinimized:
			PopupsMinimizedOption.IsChecked = true;
			break;
		case ApplicationOptions.PopupNotificationOption.Always:
			PopupsAlwaysOption.IsChecked = true;
			break;
		}
		IsAutoDismissEnabled.IsChecked = ApplicationOptions.IsAutoDismissEnabled;
		AutoDismissInterval.Text = (ApplicationOptions.AutoDismissMilliseconds / 1000).ToString();
	}

	private void PopulateEmailAlertOptions()
	{
		IsEmailAlertsEnabled.IsChecked = ApplicationOptions.IsEmailAlertEnabled;
		IsSmtpAuthenticationRequired.IsChecked = ApplicationOptions.IsEmailAuthenticationRequired;
		IsSmtpSslEnabled.IsChecked = ApplicationOptions.IsEmailSslEnabled;
		SmtpServer.Text = ApplicationOptions.EmailServer;
		SmtpPort.Text = ApplicationOptions.EmailPort;
		SmtpUsername.Text = ApplicationOptions.EmailUser;
		SmtpPassword.Password = ApplicationOptions.EmailPassword;
		EmailRecipientAddress.Text = ApplicationOptions.EmailRecipient;
		EmailFromAddress.Text = ApplicationOptions.EmailFromAddress;
	}

	private void PopulateAudioAlertOptions()
	{
		IsAudioDownAlertEnabled.IsChecked = ApplicationOptions.IsAudioDownAlertEnabled;
		AudioDownFilePath.Text = ApplicationOptions.AudioDownFilePath;
		IsAudioUpAlertEnabled.IsChecked = ApplicationOptions.IsAudioUpAlertEnabled;
		AudioUpFilePath.Text = ApplicationOptions.AudioUpFilePath;
	}

	private void PopulateLogOutputOptions()
	{
		LogPath.Text = ApplicationOptions.LogPath;
		IsLogOutputEnabled.IsChecked = ApplicationOptions.IsLogOutputEnabled;
		LogStatusChangesPath.Text = ApplicationOptions.LogStatusChangesPath;
		IsLogStatusChangesEnabled.IsChecked = ApplicationOptions.IsLogStatusChangesEnabled;
	}

	private void PopulateAdvancedOptions()
	{
		TTL.Text = ApplicationOptions.TTL.ToString();
		DontFragment.IsChecked = ApplicationOptions.DontFragment;
		if (ApplicationOptions.UseCustomBuffer)
		{
			UseCustomPacketOption.IsChecked = true;
			PacketData.Text = Encoding.ASCII.GetString(ApplicationOptions.Buffer);
		}
		else
		{
			PacketSizeOption.IsChecked = true;
			PacketSize.Text = ApplicationOptions.Buffer.Length.ToString();
		}
		UpdateByteCount();
	}

	private void PopulateServicePortsOptions()
	{
		ServiceTcpPorts.Text = ApplicationOptions.ServiceTcpPorts;
		ServiceUdpPorts.Text = ApplicationOptions.ServiceUdpPorts;
	}

	private void PopulateDisplayOptions()
	{
		IsAlwaysOnTopEnabled.IsChecked = ApplicationOptions.IsAlwaysOnTopEnabled;
		IsMinimizeToTrayEnabled.IsChecked = ApplicationOptions.IsMinimizeToTrayEnabled;
		IsExitToTrayEnabled.IsChecked = ApplicationOptions.IsExitToTrayEnabled;
		switch (ApplicationOptions.ProbeDisplayMode)
		{
		case PingDisplayMode.Log:
			PingDisplayLogOption.IsChecked = true;
			break;
		case PingDisplayMode.Graph:
			PingDisplayGraphOption.IsChecked = true;
			break;
		default:
			PingDisplayBothOption.IsChecked = true;
			break;
		}
	}

	private void PopulateLayoutOptions()
	{
		BackgroundColor_Probe_Inactive.Text = ApplicationOptions.BackgroundColor_Probe_Inactive;
		BackgroundColor_Probe_Up.Text = ApplicationOptions.BackgroundColor_Probe_Up;
		BackgroundColor_Probe_Down.Text = ApplicationOptions.BackgroundColor_Probe_Down;
		BackgroundColor_Probe_Error.Text = ApplicationOptions.BackgroundColor_Probe_Error;
		BackgroundColor_Probe_Indeterminate.Text = ApplicationOptions.BackgroundColor_Probe_Indeterminate;
		ForegroundColor_Probe_Inactive.Text = ApplicationOptions.ForegroundColor_Probe_Inactive;
		ForegroundColor_Probe_Up.Text = ApplicationOptions.ForegroundColor_Probe_Up;
		ForegroundColor_Probe_Down.Text = ApplicationOptions.ForegroundColor_Probe_Down;
		ForegroundColor_Probe_Error.Text = ApplicationOptions.ForegroundColor_Probe_Error;
		ForegroundColor_Probe_Indeterminate.Text = ApplicationOptions.ForegroundColor_Probe_Indeterminate;
		ForegroundColor_Stats_Inactive.Text = ApplicationOptions.ForegroundColor_Stats_Inactive;
		ForegroundColor_Stats_Up.Text = ApplicationOptions.ForegroundColor_Stats_Up;
		ForegroundColor_Stats_Down.Text = ApplicationOptions.ForegroundColor_Stats_Down;
		ForegroundColor_Stats_Error.Text = ApplicationOptions.ForegroundColor_Stats_Error;
		ForegroundColor_Stats_Indeterminate.Text = ApplicationOptions.ForegroundColor_Stats_Inactive;
		ForegroundColor_Alias_Inactive.Text = ApplicationOptions.ForegroundColor_Alias_Inactive;
		ForegroundColor_Alias_Up.Text = ApplicationOptions.ForegroundColor_Alias_Up;
		ForegroundColor_Alias_Down.Text = ApplicationOptions.ForegroundColor_Alias_Down;
		ForegroundColor_Alias_Error.Text = ApplicationOptions.ForegroundColor_Alias_Error;
		ForegroundColor_Alias_Indeterminate.Text = ApplicationOptions.ForegroundColor_Alias_Indeterminate;
	}

	private void OK_Click(object sender, RoutedEventArgs e)
	{
		if (SaveGeneralOptions() && SaveNotificationOptions() && SaveEmailAlertOptions() && SaveAudioAlertOptions() && SaveLogOutputOptions() && SaveAdvancedOptions() && SaveServicePortsOptions() && SaveLayoutOptions() && SaveDisplayOptions())
		{
			if (SaveAsDefaults.IsChecked == true)
			{
				Configuration.WriteConfigurationOptions();
			}
			base.DialogResult = true;
		}
	}

	private bool SaveServicePortsOptions()
	{
		if (!IsValidPortList(ServiceTcpPorts.Text))
		{
			ShowError("Please enter a valid comma-separated list of TCP ports (1-65535).", ServicePortsTab, ServiceTcpPorts);
			return false;
		}
		if (!IsValidPortList(ServiceUdpPorts.Text))
		{
			ShowError("Please enter a valid comma-separated list of UDP ports (1-65535).", ServicePortsTab, ServiceUdpPorts);
			return false;
		}
		ApplicationOptions.ServiceTcpPorts = ServiceTcpPorts.Text.Trim();
		ApplicationOptions.ServiceUdpPorts = ServiceUdpPorts.Text.Trim();
		return true;
	}

	private static bool IsValidPortList(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return true;
		}
		string[] array = text.Split(',');
		foreach (string s in array)
		{
			if (!int.TryParse(s.Trim(), out var port) || port < 1 || port > 65535)
			{
				return false;
			}
		}
		return true;
	}

	private bool SaveGeneralOptions()
	{
		if (PingInterval.Text.Length == 0)
		{
			ShowError("Please enter a valid ping interval.", GeneralTab, PingInterval);
			return false;
		}
		if (PingTimeout.Text.Length == 0)
		{
			ShowError("Please enter a valid ping timeout.", GeneralTab, PingTimeout);
			return false;
		}
		if (AlertThreshold.Text.Length == 0)
		{
			ShowError("Please enter a valid alert threshold.", GeneralTab, AlertThreshold);
			return false;
		}
		int num = 1000;
		switch (PingIntervalUnits.Text)
		{
		case "seconds":
			num = 1000;
			break;
		case "minutes":
			num = 60000;
			break;
		case "hours":
			num = 3600000;
			break;
		}
		int pingInterval = ((!int.TryParse(PingInterval.Text, out pingInterval) || pingInterval <= 0 || pingInterval > 86400) ? 2000 : (pingInterval * num));
		ApplicationOptions.PingInterval = pingInterval;
		int pingTimeout = ((!int.TryParse(PingTimeout.Text, out pingTimeout) || pingTimeout <= 0 || pingTimeout > 60) ? 2000 : (pingTimeout * 1000));
		ApplicationOptions.PingTimeout = pingTimeout;
		if (!int.TryParse(AlertThreshold.Text, out var result) || result <= 0 || result > 60)
		{
			result = 1;
		}
		ApplicationOptions.AlertThreshold = result;
		ApplicationOptions.InitialStartMode = (ApplicationOptions.StartMode)StartupMode.SelectedIndex;
		switch (StartupMode.SelectedIndex)
		{
		case 0:
		case 1:
		{
			if (int.TryParse(InitialProbeCount.Text, out var result2))
			{
				if (result2 < 1)
				{
					result2 = 1;
				}
				else if (result2 > 20)
				{
					result2 = 2;
				}
			}
			else
			{
				result2 = 2;
			}
			ApplicationOptions.InitialProbeCount = result2;
			if (int.TryParse(InitialColumnCount.Text, out result2))
			{
				if (result2 < 1)
				{
					result2 = 1;
				}
				else if (result2 > 10)
				{
					result2 = 10;
				}
			}
			else
			{
				result2 = 2;
			}
			ApplicationOptions.InitialColumnCount = result2;
			break;
		}
		case 2:
			ApplicationOptions.InitialFavorite = InitialFavorite.Text;
			break;
		}
		return true;
	}

	private bool SaveNotificationOptions()
	{
		if (IsAutoDismissEnabled.IsChecked == true)
		{
			if (!int.TryParse(AutoDismissInterval.Text, out var result) || result <= 0 || result >= 100)
			{
				ShowError("Please enter a valid number of seconds for the auto-dismiss interval.", PopupAlertsTab, AutoDismissInterval);
				return false;
			}
			ApplicationOptions.AutoDismissMilliseconds = result * 1000;
			ApplicationOptions.IsAutoDismissEnabled = true;
		}
		else
		{
			ApplicationOptions.IsAutoDismissEnabled = false;
		}
		if (PopupsMinimizedOption.IsChecked == true)
		{
			ApplicationOptions.PopupOption = ApplicationOptions.PopupNotificationOption.WhenMinimized;
		}
		else if (PopupsAlwaysOption.IsChecked == true)
		{
			ApplicationOptions.PopupOption = ApplicationOptions.PopupNotificationOption.Always;
		}
		else
		{
			ApplicationOptions.PopupOption = ApplicationOptions.PopupNotificationOption.Never;
		}
		return true;
	}

	private bool SaveAdvancedOptions()
	{
		Regex regex = new Regex("^\\d+$");
		if (!regex.IsMatch(TTL.Text) || int.Parse(TTL.Text) < 1 || int.Parse(TTL.Text) > 255)
		{
			ShowError("Please enter a valid time to live (TTL) between 1 and 255.", AdvancedTab, TTL);
			return false;
		}
		ApplicationOptions.TTL = int.Parse(TTL.Text);
		if (PacketSizeOption.IsChecked == true)
		{
			if (!regex.IsMatch(PacketSize.Text) || int.Parse(PacketSize.Text) < 0 || int.Parse(PacketSize.Text) > 65500)
			{
				ShowError("Please enter a valid data size between 0 and 65,500.", AdvancedTab, PacketSize);
				return false;
			}
			ApplicationOptions.Buffer = new byte[int.Parse(PacketSize.Text)];
			ApplicationOptions.UseCustomBuffer = false;
			if (ApplicationOptions.Buffer.Length >= 33)
			{
				Buffer.BlockCopy(Encoding.ASCII.GetBytes(Constants.DefaultIcmpData), 0, ApplicationOptions.Buffer, 0, 33);
			}
		}
		else
		{
			ApplicationOptions.Buffer = Encoding.ASCII.GetBytes(PacketData.Text);
			ApplicationOptions.UseCustomBuffer = true;
		}
		if (DontFragment.IsChecked == true)
		{
			ApplicationOptions.DontFragment = true;
		}
		else
		{
			ApplicationOptions.DontFragment = false;
		}
		ApplicationOptions.UpdatePingOptions();
		return true;
	}

	private bool SaveEmailAlertOptions()
	{
		if (IsEmailAlertsEnabled.IsChecked == true)
		{
			Regex regex = new Regex("^\\d+$");
			if (SmtpServer.Text.Length == 0)
			{
				ShowError("Please enter a valid SMTP server address.", EmailAlertsTab, SmtpServer);
				return false;
			}
			if (SmtpPort.Text.Length == 0 || !regex.IsMatch(SmtpPort.Text))
			{
				ShowError("Please enter a valid port number for your SMTP server.", EmailAlertsTab, SmtpPort);
				return false;
			}
			if (EmailRecipientAddress.Text.Length == 0)
			{
				ShowError("Please enter a valid recipient email address. This address will receive email alerts from DoMping.", EmailAlertsTab, EmailRecipientAddress);
				return false;
			}
			if (EmailFromAddress.Text.Length == 0)
			{
				ShowError("Please enter a valid 'from' address. This address appears as the sender for any alerts that are sent.", EmailAlertsTab, EmailFromAddress);
				return false;
			}
			if (IsSmtpAuthenticationRequired.IsChecked == true)
			{
				ApplicationOptions.IsEmailAuthenticationRequired = true;
				if (SmtpUsername.Text.Length == 0)
				{
					ShowError("Please enter a valid username for your mail server.", EmailAlertsTab, SmtpUsername);
					return false;
				}
			}
			else
			{
				ApplicationOptions.IsEmailAuthenticationRequired = false;
				SmtpUsername.Text = string.Empty;
				SmtpPassword.Password = string.Empty;
			}
			ApplicationOptions.IsEmailAlertEnabled = true;
			ApplicationOptions.EmailServer = SmtpServer.Text;
			ApplicationOptions.EmailPort = SmtpPort.Text;
			ApplicationOptions.EmailUser = SmtpUsername.Text;
			ApplicationOptions.EmailPassword = SmtpPassword.Password;
			ApplicationOptions.EmailRecipient = EmailRecipientAddress.Text;
			ApplicationOptions.EmailFromAddress = EmailFromAddress.Text;
			ApplicationOptions.IsEmailSslEnabled = IsSmtpSslEnabled.IsChecked == true;
			return true;
		}
		ApplicationOptions.IsEmailAlertEnabled = false;
		return true;
	}

	private bool SaveAudioAlertOptions()
	{
		if (IsAudioDownAlertEnabled.IsChecked == true)
		{
			try
			{
				if (Path.GetFileName(AudioDownFilePath.Text).IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || !File.Exists(AudioDownFilePath.Text) || Path.GetFileName(AudioDownFilePath.Text).Length < 1)
				{
					throw new Exception();
				}
			}
			catch
			{
				ShowError("The specified path does not exist. Please enter a valid path.", AudioAlertTab, AudioDownFilePath);
				return false;
			}
			ApplicationOptions.IsAudioDownAlertEnabled = true;
			ApplicationOptions.AudioDownFilePath = AudioDownFilePath.Text;
		}
		else
		{
			ApplicationOptions.IsAudioDownAlertEnabled = false;
		}
		if (IsAudioUpAlertEnabled.IsChecked == true)
		{
			try
			{
				if (Path.GetFileName(AudioUpFilePath.Text).IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || !File.Exists(AudioUpFilePath.Text) || Path.GetFileName(AudioUpFilePath.Text).Length < 1)
				{
					throw new Exception();
				}
			}
			catch
			{
				ShowError("The specified path does not exist. Please enter a valid path.", AudioAlertTab, AudioUpFilePath);
				return false;
			}
			ApplicationOptions.IsAudioUpAlertEnabled = true;
			ApplicationOptions.AudioUpFilePath = AudioUpFilePath.Text;
		}
		else
		{
			ApplicationOptions.IsAudioUpAlertEnabled = false;
		}
		return true;
	}

	private bool SaveLogOutputOptions()
	{
		if (IsLogOutputEnabled.IsChecked == true)
		{
			if (!Directory.Exists(LogPath.Text))
			{
				ShowError("The specified path does not exist.  Please enter a valid path.", LogOutputTab, LogPath);
				return false;
			}
			ApplicationOptions.IsLogOutputEnabled = true;
			ApplicationOptions.LogPath = LogPath.Text;
		}
		else
		{
			ApplicationOptions.IsLogOutputEnabled = false;
		}
		if (IsLogStatusChangesEnabled.IsChecked == true)
		{
			try
			{
				if (Path.GetFileName(LogStatusChangesPath.Text).IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || !Directory.Exists(Path.GetDirectoryName(LogStatusChangesPath.Text)) || Path.GetFileName(LogStatusChangesPath.Text).Length < 1)
				{
					throw new Exception();
				}
			}
			catch
			{
				ShowError("The specified path does not exist.  Please enter a valid path.", LogOutputTab, LogStatusChangesPath);
				return false;
			}
			ApplicationOptions.IsLogStatusChangesEnabled = true;
			ApplicationOptions.LogStatusChangesPath = LogStatusChangesPath.Text;
		}
		else
		{
			ApplicationOptions.IsLogStatusChangesEnabled = false;
		}
		return true;
	}

	private bool SaveDisplayOptions()
	{
		ApplicationOptions.IsAlwaysOnTopEnabled = IsAlwaysOnTopEnabled.IsChecked == true;
		ApplicationOptions.IsMinimizeToTrayEnabled = IsMinimizeToTrayEnabled.IsChecked == true;
		ApplicationOptions.IsExitToTrayEnabled = IsExitToTrayEnabled.IsChecked == true;
		if (PingDisplayLogOption.IsChecked == true)
		{
			ApplicationOptions.ProbeDisplayMode = PingDisplayMode.Log;
		}
		else if (PingDisplayGraphOption.IsChecked == true)
		{
			ApplicationOptions.ProbeDisplayMode = PingDisplayMode.Graph;
		}
		else
		{
			ApplicationOptions.ProbeDisplayMode = PingDisplayMode.Both;
		}
		return true;
	}

	private bool SaveLayoutOptions()
	{
		foreach (Visual child in ColorsDockPanel.GetChildren())
		{
			if (child is System.Windows.Controls.TextBox && !Util.IsValidHtmlColor(((System.Windows.Controls.TextBox)child).Text))
			{
				ShowError("Please enter a valid HTML color code.  Accepted formats are #RGB, #RRGGBB, and #AARRGGBB.  Example: #3266CF", LayoutTab, (System.Windows.Controls.TextBox)child);
				((System.Windows.Controls.TextBox)child).SelectAll();
				return false;
			}
		}
		ApplicationOptions.BackgroundColor_Probe_Inactive = BackgroundColor_Probe_Inactive.Text;
		ApplicationOptions.BackgroundColor_Probe_Up = BackgroundColor_Probe_Up.Text;
		ApplicationOptions.BackgroundColor_Probe_Down = BackgroundColor_Probe_Down.Text;
		ApplicationOptions.BackgroundColor_Probe_Indeterminate = BackgroundColor_Probe_Indeterminate.Text;
		ApplicationOptions.BackgroundColor_Probe_Error = BackgroundColor_Probe_Error.Text;
		ApplicationOptions.ForegroundColor_Probe_Inactive = ForegroundColor_Probe_Inactive.Text;
		ApplicationOptions.ForegroundColor_Probe_Up = ForegroundColor_Probe_Up.Text;
		ApplicationOptions.ForegroundColor_Probe_Down = ForegroundColor_Probe_Down.Text;
		ApplicationOptions.ForegroundColor_Probe_Indeterminate = ForegroundColor_Probe_Indeterminate.Text;
		ApplicationOptions.ForegroundColor_Probe_Error = ForegroundColor_Probe_Error.Text;
		ApplicationOptions.ForegroundColor_Stats_Inactive = ForegroundColor_Stats_Inactive.Text;
		ApplicationOptions.ForegroundColor_Stats_Up = ForegroundColor_Stats_Up.Text;
		ApplicationOptions.ForegroundColor_Stats_Down = ForegroundColor_Stats_Down.Text;
		ApplicationOptions.ForegroundColor_Stats_Indeterminate = ForegroundColor_Stats_Indeterminate.Text;
		ApplicationOptions.ForegroundColor_Stats_Error = ForegroundColor_Stats_Error.Text;
		ApplicationOptions.ForegroundColor_Alias_Inactive = ForegroundColor_Alias_Inactive.Text;
		ApplicationOptions.ForegroundColor_Alias_Up = ForegroundColor_Alias_Up.Text;
		ApplicationOptions.ForegroundColor_Alias_Down = ForegroundColor_Alias_Down.Text;
		ApplicationOptions.ForegroundColor_Alias_Indeterminate = ForegroundColor_Alias_Indeterminate.Text;
		ApplicationOptions.ForegroundColor_Alias_Error = ForegroundColor_Alias_Error.Text;
		return true;
	}

	private void NumericTextbox_PreviewTextInput(object sender, TextCompositionEventArgs e)
	{
		if (new Regex("[^0-9.-]+").IsMatch(e.Text))
		{
			e.Handled = true;
		}
	}

	private void HtmlColor_PreviewTextInput(object sender, TextCompositionEventArgs e)
	{
		if (!new Regex("[#a-fA-F0-9]").IsMatch(e.Text))
		{
			e.Handled = true;
		}
	}

	private void EmailRecipientAddress_LostFocus(object sender, RoutedEventArgs e)
	{
		if (EmailFromAddress.Text.Length == 0 && EmailRecipientAddress.Text.IndexOf('@') >= 0)
		{
			EmailFromAddress.Text = "DoMping" + EmailRecipientAddress.Text.Substring(EmailRecipientAddress.Text.IndexOf('@'));
		}
	}

	private void IsEmailAlertsEnabled_Click(object sender, RoutedEventArgs e)
	{
		if (IsEmailAlertsEnabled.IsChecked == true && SmtpServer.Text.Length == 0)
		{
			SmtpServer.Focus();
		}
	}

	private void IsSmtpAuthenticationRequired_Click(object sender, RoutedEventArgs e)
	{
		if (IsSmtpAuthenticationRequired.IsChecked == true)
		{
			SmtpUsername.Focus();
		}
	}

	private async void TestEmail_Click(object sender, RoutedEventArgs e)
	{
		TestEmailButton.IsEnabled = false;
		TestEmailButton.Content = "Testing...";
		string serverAddress = SmtpServer.Text;
		string serverPort = SmtpPort.Text;
		bool isSslEnabled = IsSmtpSslEnabled.IsChecked == true;
		bool isAuthRequired = IsSmtpAuthenticationRequired.IsChecked == true;
		string username = SmtpUsername.Text;
		SecureString password = SmtpPassword.SecurePassword;
		string mailFrom = EmailFromAddress.Text;
		string mailRecipient = EmailRecipientAddress.Text;
		await Task.Run(delegate
		{
			try
			{
				Util.SendTestEmail(serverAddress, serverPort, isSslEnabled, isAuthRequired, username, password, mailFrom, mailRecipient);
			}
			catch (Exception ex)
			{
				Exception ex2 = ex;
				Exception ex3 = ex2;
				System.Windows.Application.Current.Dispatcher.BeginInvoke((Action)delegate
				{
					ShowError(ex3.Message, EmailAlertsTab, TestEmailButton);
				});
			}
		});
		TestEmailButton.IsEnabled = true;
		TestEmailButton.Content = "Test";
	}

	private void BrowseLogPath_Click(object sender, RoutedEventArgs e)
	{
		using FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
		folderBrowserDialog.Description = "Select a location for the log files.";
		if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
		{
			LogPath.Text = folderBrowserDialog.SelectedPath;
		}
	}

	private void BrowseLogStatusChangesPath_Click(object sender, RoutedEventArgs e)
	{
		using FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
		folderBrowserDialog.Description = "Select a location for the log files.";
		if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
		{
			LogStatusChangesPath.Text = folderBrowserDialog.SelectedPath + "\\domping-status.txt";
		}
	}

	private void AudioDownBrowse_Click(object sender, RoutedEventArgs e)
	{
		AudioFileBrowse(AudioDownFilePath);
	}

	private void AudioUpBrowse_Click(object sender, RoutedEventArgs e)
	{
		AudioFileBrowse(AudioUpFilePath);
	}

	private void AudioFileBrowse(System.Windows.Controls.TextBox tb)
	{
		using OpenFileDialog openFileDialog = new OpenFileDialog();
		openFileDialog.Title = "Select an audio file";
		openFileDialog.RestoreDirectory = true;
		openFileDialog.Multiselect = false;
		openFileDialog.Filter = "WAV files (*.wav)|*.wav|All files|*.*";
		openFileDialog.DefaultExt = ".wav";
		if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
		{
			tb.Text = openFileDialog.FileName;
		}
	}

	private void AudioDownPlay_Click(object sender, RoutedEventArgs e)
	{
		AudioFilePlay(AudioDownFilePath.Text);
	}

	private void AudioUpPlay_Click(object sender, RoutedEventArgs e)
	{
		AudioFilePlay(AudioUpFilePath.Text);
	}

	private void AudioFilePlay(string path)
	{
		try
		{
			using SoundPlayer soundPlayer = new SoundPlayer(path);
			soundPlayer.Play();
		}
		catch
		{
			ShowError("Unable to play audio file.", AudioAlertTab, AudioAlertTab);
		}
	}

	private void IsAudioDownAlertEnabled_Click(object sender, RoutedEventArgs e)
	{
		if (AudioDownFilePath.Text.Length == 0 && File.Exists(Environment.ExpandEnvironmentVariables("%WINDIR%\\Media\\Windows Notify Email.wav")))
		{
			AudioDownFilePath.Text = Environment.ExpandEnvironmentVariables("%WINDIR%\\Media\\Windows Notify Email.wav");
		}
	}

	private void IsAudioUpAlertEnabled_Click(object sender, RoutedEventArgs e)
	{
		if (AudioUpFilePath.Text.Length == 0 && File.Exists(Environment.ExpandEnvironmentVariables("%WINDIR%\\Media\\Windows Unlock.wav")))
		{
			AudioUpFilePath.Text = Environment.ExpandEnvironmentVariables("%WINDIR%\\Media\\Windows Unlock.wav");
		}
	}

	private void UpdateByteCount()
	{
		Regex regex = new Regex("^\\d+$");
		if (PacketSizeOption.IsChecked == true)
		{
			if (PacketSize != null && regex.IsMatch(PacketSize.Text))
			{
				Bytes.Text = (int.Parse(PacketSize.Text) + 28).ToString();
			}
			else
			{
				Bytes.Text = "?";
			}
		}
		else
		{
			Bytes.Text = (PacketData.Text.Length + 28).ToString();
		}
	}

	private void PacketData_TextChanged(object sender, TextChangedEventArgs e)
	{
		UpdateByteCount();
	}

	private void PacketSizeOption_Checked(object sender, RoutedEventArgs e)
	{
		if (base.IsLoaded)
		{
			UpdateByteCount();
		}
	}

	private void UseCustomPacketOption_Checked(object sender, RoutedEventArgs e)
	{
		if (base.IsLoaded)
		{
			UpdateByteCount();
		}
	}

	private void RestoreDefaultColors_Click(object sender, RoutedEventArgs e)
	{
		BackgroundColor_Probe_Inactive.Text = "#eceafa";
		BackgroundColor_Probe_Up.Text = "#859900";
		BackgroundColor_Probe_Down.Text = "#dc322f";
		BackgroundColor_Probe_Error.Text = "#b58900";
		BackgroundColor_Probe_Indeterminate.Text = "#dfdf00";
		ForegroundColor_Probe_Inactive.Text = "#839496";
		ForegroundColor_Probe_Up.Text = "#002b36";
		ForegroundColor_Probe_Down.Text = "#002b36";
		ForegroundColor_Probe_Error.Text = "#000000";
		ForegroundColor_Probe_Indeterminate.Text = "#002b36";
		ForegroundColor_Stats_Inactive.Text = "#657b83";
		ForegroundColor_Stats_Up.Text = "#fdf6e3";
		ForegroundColor_Stats_Down.Text = "#fdf6e3";
		ForegroundColor_Stats_Error.Text = "#ffffff";
		ForegroundColor_Stats_Indeterminate.Text = "#657b83";
		ForegroundColor_Alias_Inactive.Text = "#000000";
		ForegroundColor_Alias_Up.Text = "#ffff00";
		ForegroundColor_Alias_Down.Text = "#ffff00";
		ForegroundColor_Alias_Error.Text = "#ffff00";
		ForegroundColor_Alias_Indeterminate.Text = "#ffffff";
	}

	private void Window_SourceInitialized(object sender, EventArgs e)
	{
		IntPtr handle = new WindowInteropHelper(this).Handle;
		SetWindowLong(handle, -16, GetWindowLong(handle, -16) & -65537 & -131073);
	}
}
