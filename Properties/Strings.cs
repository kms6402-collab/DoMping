using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;

namespace DoMping.Properties;

[GeneratedCode("System.Resources.Tools.StronglyTypedResourceBuilder", "16.0.0.0")]
[DebuggerNonUserCode]
[CompilerGenerated]
public class Strings
{
	private static ResourceManager resourceMan;

	private static CultureInfo resourceCulture;

	[EditorBrowsable(EditorBrowsableState.Advanced)]
	public static ResourceManager ResourceManager
	{
		get
		{
			if (resourceMan == null)
			{
				resourceMan = new ResourceManager("DoMping.Properties.Strings", typeof(Strings).Assembly);
			}
			return resourceMan;
		}
	}

	[EditorBrowsable(EditorBrowsableState.Advanced)]
	public static CultureInfo Culture
	{
		get
		{
			return resourceCulture;
		}
		set
		{
			resourceCulture = value;
		}
	}

	public static string Button_Ping => ResourceManager.GetString("Button_Ping", resourceCulture);

	public static string Button_Stop => ResourceManager.GetString("Button_Stop", resourceCulture);

	public static string DialogButton_Cancel => ResourceManager.GetString("DialogButton_Cancel", resourceCulture);

	public static string DialogButton_Edit => ResourceManager.GetString("DialogButton_Edit", resourceCulture);

	public static string DialogButton_New => ResourceManager.GetString("DialogButton_New", resourceCulture);

	public static string DialogButton_OK => ResourceManager.GetString("DialogButton_OK", resourceCulture);

	public static string DialogButton_Overwrite => ResourceManager.GetString("DialogButton_Overwrite", resourceCulture);

	public static string DialogButton_Remove => ResourceManager.GetString("DialogButton_Remove", resourceCulture);

	public static string DialogButton_Save => ResourceManager.GetString("DialogButton_Save", resourceCulture);

	public static string DialogTitle_ConfirmDelete => ResourceManager.GetString("DialogTitle_ConfirmDelete", resourceCulture);

	public static string DialogTitle_Error => ResourceManager.GetString("DialogTitle_Error", resourceCulture);

	public static string DialogTitle_Warning => ResourceManager.GetString("DialogTitle_Warning", resourceCulture);

	public static string EditAlias_AliasFor => ResourceManager.GetString("EditAlias_AliasFor", resourceCulture);

	public static string Email_Down => ResourceManager.GetString("Email_Down", resourceCulture);

	public static string Email_Error => ResourceManager.GetString("Email_Error", resourceCulture);

	public static string Email_Host => ResourceManager.GetString("Email_Host", resourceCulture);

	public static string Email_Up => ResourceManager.GetString("Email_Up", resourceCulture);

	public static string Email_Verb => ResourceManager.GetString("Email_Verb", resourceCulture);

	public static string Error_AddAlias => ResourceManager.GetString("Error_AddAlias", resourceCulture);

	public static string Error_CreateConfig => ResourceManager.GetString("Error_CreateConfig", resourceCulture);

	public static string Error_CreateDirectory => ResourceManager.GetString("Error_CreateDirectory", resourceCulture);

	public static string Error_DeleteAlias => ResourceManager.GetString("Error_DeleteAlias", resourceCulture);

	public static string Error_FailedToLaunch => ResourceManager.GetString("Error_FailedToLaunch", resourceCulture);

	public static string Error_LoadAlias => ResourceManager.GetString("Error_LoadAlias", resourceCulture);

	public static string Error_LoadConfig => ResourceManager.GetString("Error_LoadConfig", resourceCulture);

	public static string Error_ReadConfig => ResourceManager.GetString("Error_ReadConfig", resourceCulture);

	public static string Error_UpgradeConfig => ResourceManager.GetString("Error_UpgradeConfig", resourceCulture);

	public static string Error_WindowTitle => ResourceManager.GetString("Error_WindowTitle", resourceCulture);

	public static string Error_WriteConfig => ResourceManager.GetString("Error_WriteConfig", resourceCulture);

	public static string ManageAliases_Alias => ResourceManager.GetString("ManageAliases_Alias", resourceCulture);

	public static string ManageAliases_Header => ResourceManager.GetString("ManageAliases_Header", resourceCulture);

	public static string ManageAliases_Host => ResourceManager.GetString("ManageAliases_Host", resourceCulture);

	public static string ManageAliases_Warn_DeleteA => ResourceManager.GetString("ManageAliases_Warn_DeleteA", resourceCulture);

	public static string ManageAliases_Warn_DeleteB => ResourceManager.GetString("ManageAliases_Warn_DeleteB", resourceCulture);

	public static string ManageAliases_WindowTitle => ResourceManager.GetString("ManageAliases_WindowTitle", resourceCulture);

	public static string ManageFavorites_Header => ResourceManager.GetString("ManageFavorites_Header", resourceCulture);

	public static string ManageFavorites_Warn_DeleteA => ResourceManager.GetString("ManageFavorites_Warn_DeleteA", resourceCulture);

	public static string ManageFavorites_Warn_DeleteB => ResourceManager.GetString("ManageFavorites_Warn_DeleteB", resourceCulture);

	public static string ManageFavorites_WindowTitle => ResourceManager.GetString("ManageFavorites_WindowTitle", resourceCulture);

	public static string Menu_Aliases => ResourceManager.GetString("Menu_Aliases", resourceCulture);

	public static string Menu_FavoriteSets => ResourceManager.GetString("Menu_FavoriteSets", resourceCulture);

	public static string Menu_FloodHost => ResourceManager.GetString("Menu_FloodHost", resourceCulture);

	public static string Menu_Help => ResourceManager.GetString("Menu_Help", resourceCulture);

	public static string Menu_ManageAliases => ResourceManager.GetString("Menu_ManageAliases", resourceCulture);

	public static string Menu_ManageFavorites => ResourceManager.GetString("Menu_ManageFavorites", resourceCulture);

	public static string Menu_Options => ResourceManager.GetString("Menu_Options", resourceCulture);

	public static string Menu_PopupAlerts => ResourceManager.GetString("Menu_PopupAlerts", resourceCulture);

	public static string Menu_SaveToFavorites => ResourceManager.GetString("Menu_SaveToFavorites", resourceCulture);

	public static string Menu_StatusHistory => ResourceManager.GetString("Menu_StatusHistory", resourceCulture);

	public static string Menu_Traceroute => ResourceManager.GetString("Menu_Traceroute", resourceCulture);

	public static string Milliseconds_Symbol => ResourceManager.GetString("Milliseconds_Symbol", resourceCulture);

	public static string NewAlias_Alias => ResourceManager.GetString("NewAlias_Alias", resourceCulture);

	public static string NewAlias_Error_InvalidAlias => ResourceManager.GetString("NewAlias_Error_InvalidAlias", resourceCulture);

	public static string NewAlias_Error_InvalidHost => ResourceManager.GetString("NewAlias_Error_InvalidHost", resourceCulture);

	public static string NewAlias_Header => ResourceManager.GetString("NewAlias_Header", resourceCulture);

	public static string NewAlias_Host => ResourceManager.GetString("NewAlias_Host", resourceCulture);

	public static string NewFavorite_Error_InvalidName => ResourceManager.GetString("NewFavorite_Error_InvalidName", resourceCulture);

	public static string NewFavorite_Header => ResourceManager.GetString("NewFavorite_Header", resourceCulture);

	public static string NewFavorite_SelectedHostsHeader => ResourceManager.GetString("NewFavorite_SelectedHostsHeader", resourceCulture);

	public static string NewFavorite_Title => ResourceManager.GetString("NewFavorite_Title", resourceCulture);

	public static string NewFavorite_Warn_AlreadyExists => ResourceManager.GetString("NewFavorite_Warn_AlreadyExists", resourceCulture);

	public static string Notification_Always => ResourceManager.GetString("Notification_Always", resourceCulture);

	public static string Notification_Never => ResourceManager.GetString("Notification_Never", resourceCulture);

	public static string Notification_WhenMinimized => ResourceManager.GetString("Notification_WhenMinimized", resourceCulture);

	public static string Probe_Error => ResourceManager.GetString("Probe_Error", resourceCulture);

	public static string Probe_HostUnreachable => ResourceManager.GetString("Probe_HostUnreachable", resourceCulture);

	public static string Probe_InvalidPort => ResourceManager.GetString("Probe_InvalidPort", resourceCulture);

	public static string Probe_NetUnreachable => ResourceManager.GetString("Probe_NetUnreachable", resourceCulture);

	public static string Probe_Port => ResourceManager.GetString("Probe_Port", resourceCulture);

	public static string Probe_PortClosed => ResourceManager.GetString("Probe_PortClosed", resourceCulture);

	public static string Probe_PortOpen => ResourceManager.GetString("Probe_PortOpen", resourceCulture);

	public static string Probe_Reply => ResourceManager.GetString("Probe_Reply", resourceCulture);

	public static string Probe_StartPing => ResourceManager.GetString("Probe_StartPing", resourceCulture);

	public static string Probe_Stat_Lost => ResourceManager.GetString("Probe_Stat_Lost", resourceCulture);

	public static string Probe_Stat_Received => ResourceManager.GetString("Probe_Stat_Received", resourceCulture);

	public static string Probe_Stat_Sent => ResourceManager.GetString("Probe_Stat_Sent", resourceCulture);

	public static string Probe_Timeout => ResourceManager.GetString("Probe_Timeout", resourceCulture);

	public static string Probe_UnableToResolve => ResourceManager.GetString("Probe_UnableToResolve", resourceCulture);

	public static string Probe_Unreachable => ResourceManager.GetString("Probe_Unreachable", resourceCulture);

	public static string StatusChange_Down => ResourceManager.GetString("StatusChange_Down", resourceCulture);

	public static string StatusChange_Error => ResourceManager.GetString("StatusChange_Error", resourceCulture);

	public static string StatusChange_LatencyHigh => ResourceManager.GetString("StatusChange_LatencyHigh", resourceCulture);

	public static string StatusChange_LatencyNormal => ResourceManager.GetString("StatusChange_LatencyNormal", resourceCulture);

	public static string StatusChange_Start => ResourceManager.GetString("StatusChange_Start", resourceCulture);

	public static string StatusChange_Stop => ResourceManager.GetString("StatusChange_Stop", resourceCulture);

	public static string StatusChange_Up => ResourceManager.GetString("StatusChange_Up", resourceCulture);

	public static string StatusHistory_NoChanges => ResourceManager.GetString("StatusHistory_NoChanges", resourceCulture);

	public static string StatusHistory_NoChangesDescription => ResourceManager.GetString("StatusHistory_NoChangesDescription", resourceCulture);

	public static string StatusHistory_WindowTitle => ResourceManager.GetString("StatusHistory_WindowTitle", resourceCulture);

	public static string Toolbar_Add => ResourceManager.GetString("Toolbar_Add", resourceCulture);

	public static string Toolbar_Columns => ResourceManager.GetString("Toolbar_Columns", resourceCulture);

	public static string Toolbar_StartAll => ResourceManager.GetString("Toolbar_StartAll", resourceCulture);

	public static string Toolbar_StopAll => ResourceManager.GetString("Toolbar_StopAll", resourceCulture);

	public static string Tooltip_Close => ResourceManager.GetString("Tooltip_Close", resourceCulture);

	public static string Tooltip_EditAlias => ResourceManager.GetString("Tooltip_EditAlias", resourceCulture);

	public static string Tooltip_IsolatedView => ResourceManager.GetString("Tooltip_IsolatedView", resourceCulture);

	internal Strings()
	{
	}
}
