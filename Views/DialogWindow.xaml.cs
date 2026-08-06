using System.Media;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;
using DoMping.Properties;

namespace DoMping.Views;

public partial class DialogWindow : Window, IComponentConnector
{
	public enum DialogIcon
	{
		Warning,
		Error,
		Info,
		None
	}

	public DialogWindow(DialogIcon icon, string title, string body, string confirmationText, bool isCancelButtonVisible)
	{
		InitializeComponent();
		MyTitle.Text = title;
		Body.Text = body;
		OK.Content = confirmationText;
		if (!isCancelButtonVisible)
		{
			Cancel.Visibility = Visibility.Collapsed;
		}
		switch (icon)
		{
		case DialogIcon.Warning:
			MyIcon.Source = (DrawingImage)Application.Current.Resources["icon.exclamation-triangle"];
			break;
		case DialogIcon.Error:
			MyIcon.Source = (DrawingImage)Application.Current.Resources["icon.exclamation-circle"];
			break;
		case DialogIcon.Info:
			MyIcon.Source = (DrawingImage)Application.Current.Resources["icon.info-circle"];
			break;
		case DialogIcon.None:
			MyIcon.Visibility = Visibility.Collapsed;
			break;
		}
	}

	public static DialogWindow ErrorWindow(string message)
	{
		SystemSounds.Exclamation.Play();
		return new DialogWindow(DialogIcon.Error, Strings.DialogTitle_Error, message, Strings.DialogButton_OK, isCancelButtonVisible: false);
	}

	public static DialogWindow WarningWindow(string message, string confirmButtonText)
	{
		SystemSounds.Exclamation.Play();
		return new DialogWindow(DialogIcon.Warning, Strings.DialogTitle_Warning, message, confirmButtonText, isCancelButtonVisible: true);
	}

	private void OK_Click(object sender, RoutedEventArgs e)
	{
		base.DialogResult = true;
	}
}
