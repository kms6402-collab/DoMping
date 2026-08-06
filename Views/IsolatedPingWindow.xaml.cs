using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using DoMping.Classes;

namespace DoMping.Views;

public partial class IsolatedPingWindow : Window, IComponentConnector
{
	private int SelStart;

	private int SelLength;

	public IsolatedPingWindow(Probe pingItem)
	{
		InitializeComponent();
		base.Topmost = ApplicationOptions.IsAlwaysOnTopEnabled;
		pingItem.IsolatedWindow = this;
		base.DataContext = pingItem;
	}

	private void History_TextChanged(object sender, TextChangedEventArgs e)
	{
		History.SelectionStart = SelStart;
		History.SelectionLength = SelLength;
		if (!History.IsMouseCaptureWithin && History.SelectionLength == 0)
		{
			History.ScrollToEnd();
		}
	}

	private void History_SelectionChanged(object sender, RoutedEventArgs e)
	{
		SelStart = History.SelectionStart;
		SelLength = History.SelectionLength;
	}

	private void Window_Closed(object sender, EventArgs e)
	{
		(base.DataContext as Probe).IsolatedWindow = null;
		base.DataContext = null;
	}
}
