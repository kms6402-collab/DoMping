using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;
using DoMping.Classes;

namespace DoMping.Views;

public partial class PopupNotificationWindow : Window, IComponentConnector
{
	private DispatcherTimer _AutoDismissTimer;

	public PopupNotificationWindow(ObservableCollection<StatusChangeLog> statusChangeLog)
	{
		InitializeComponent();
		ICollectionView view = new CollectionViewSource
		{
			Source = statusChangeLog
		}.View;
		view.Filter = delegate(object item)
		{
			StatusChangeLog statusChangeLog2 = item as StatusChangeLog;
			return !statusChangeLog2.HasStatusBeenCleared && statusChangeLog2.Status != ProbeStatus.Start && statusChangeLog2.Status != ProbeStatus.Stop;
		};
		StatusHistoryList.ItemsSource = view;
		((INotifyCollectionChanged)StatusHistoryList.Items).CollectionChanged += PopupNotificationWindow_CollectionChanged;
		_AutoDismissTimer = new DispatcherTimer();
		_AutoDismissTimer.Tick += AutoDismissTimer_Tick;
		if (ApplicationOptions.IsAutoDismissEnabled)
		{
			_AutoDismissTimer.Interval = TimeSpan.FromMilliseconds(ApplicationOptions.AutoDismissMilliseconds);
			_AutoDismissTimer.Start();
		}
	}

	private void AutoDismissTimer_Tick(object sender, EventArgs e)
	{
		if (ApplicationOptions.IsAutoDismissEnabled)
		{
			Close();
		}
	}

	private void PopupNotificationWindow_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
	{
		if (ApplicationOptions.IsAutoDismissEnabled)
		{
			_AutoDismissTimer.Stop();
			_AutoDismissTimer.Interval = TimeSpan.FromMilliseconds(ApplicationOptions.AutoDismissMilliseconds);
			_AutoDismissTimer.Start();
		}
		ScaleWindowSize();
		ScrollToEnd();
	}

	private void ScrollToEnd()
	{
		if (StatusHistoryList.Items.Count > 0 && VisualTreeHelper.GetChildrenCount(StatusHistoryList) > 0 && VisualTreeHelper.GetChild(StatusHistoryList, 0) is Decorator { Child: ScrollViewer child })
		{
			child.ScrollToEnd();
		}
	}

	private void ScaleWindowSize()
	{
		switch (StatusHistoryList.Items.Count)
		{
		case 1:
			base.Height = 95.0;
			break;
		case 2:
			base.Height = 110.0;
			break;
		case 3:
			base.Height = 126.0;
			break;
		case 4:
			base.Height = 147.0;
			break;
		case 5:
			base.Height = 172.0;
			break;
		}
		PositionWindow(base.Width);
	}

	private void PositionWindow(double width)
	{
		Rect workArea = SystemParameters.WorkArea;
		base.Left = workArea.Right - width;
		base.Top = workArea.Bottom - base.Height;
	}

	private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
	{
		if (Application.Current.MainWindow.WindowState == WindowState.Minimized)
		{
			Application.Current.MainWindow.WindowState = WindowState.Normal;
		}
		if (Application.Current.MainWindow.Visibility != Visibility.Visible)
		{
			Application.Current.MainWindow.Visibility = Visibility.Visible;
		}
		Application.Current.MainWindow.Focus();
	}

	private void Close_Click(object sender, RoutedEventArgs e)
	{
		Close();
	}

	private void Maximize_Click(object sender, RoutedEventArgs e)
	{
		if (Probe.StatusWindow == null || !Probe.StatusWindow.IsLoaded)
		{
			(Probe.StatusWindow = new StatusHistoryWindow(Probe.StatusChangeLog)).Show();
		}
		else if (Probe.StatusWindow.IsLoaded)
		{
			Probe.StatusWindow.Focus();
		}
		Close();
	}

	private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
	{
		PositionWindow(e.NewSize.Width);
	}
}
