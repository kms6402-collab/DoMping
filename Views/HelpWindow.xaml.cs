using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Navigation;
using DoMping.Classes;

namespace DoMping.Views;

public partial class HelpWindow : Window, IComponentConnector
{
	public static HelpWindow _OpenWindow;

	public HelpWindow()
	{
		InitializeComponent();
		base.Topmost = ApplicationOptions.IsAlwaysOnTopEnabled;
		Version version = typeof(MainWindow).Assembly.GetName().Version;
		Version.Inlines.Clear();
		Version.Inlines.Add(new Run($"Version: {version.Major}.{version.Minor}.{version.Build}"));
	}

	private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
	{
		try
		{
			Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
		}
		catch
		{
		}
		finally
		{
			e.Handled = true;
		}
	}

	private void Window_Loaded(object sender, RoutedEventArgs e)
	{
		_OpenWindow = this;
	}

	private void Window_Closed(object sender, EventArgs e)
	{
		_OpenWindow = null;
	}

	private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
	{
		if (e.Key == Key.Escape || e.Key == Key.F1)
		{
			e.Handled = true;
			Close();
		}
	}
}
