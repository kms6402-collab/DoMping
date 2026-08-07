using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using DoMping.Classes;

namespace DoMping.Views;

public partial class StatusHistoryWindow : Window, IComponentConnector
{
	[Serializable]
	public struct RECT(int left, int top, int right, int bottom)
	{
		public int Left = left;

		public int Top = top;

		public int Right = right;

		public int Bottom = bottom;
	}

	public struct MONITORINFO
	{
		public int cbSize;

		public RECT rcMonitor;

		public RECT rcWork;

		public uint dwFlags;
	}

	[Serializable]
	public struct POINT(int x, int y)
	{
		public int X = x;

		public int Y = y;
	}

	public struct MINMAXINFO
	{
		public POINT ptReserved;

		public POINT ptMaxSize;

		public POINT ptMaxPosition;

		public POINT ptMinTrackSize;

		public POINT ptMaxTrackSize;
	}

	private ICollectionView _statusHistoryView;

	private static bool IsWindowStateSet;

	private static double _Left;

	private static double _Top;

	private static double _Width;

	private static double _Height;

	private static WindowState _WindowState;

	private const int WM_GETMINMAXINFO = 36;

	private const uint MONITOR_DEFAULTTONEAREST = 2u;

	public StatusHistoryWindow(ObservableCollection<StatusChangeLog> statusChangeLog)
	{
		InitializeComponent();
		if (IsWindowStateSet)
		{
			base.WindowState = _WindowState;
			if (_Left < SystemParameters.VirtualScreenWidth)
			{
				base.Left = _Left;
			}
			base.Top = _Top;
			base.Width = _Width;
			base.Height = _Height;
		}
		RefreshMaximizeRestoreButton();
		base.Topmost = ApplicationOptions.IsAlwaysOnTopEnabled;
		_statusHistoryView = CollectionViewSource.GetDefaultView(statusChangeLog);
		_statusHistoryView.Filter = AddressFilter;
		StatusHistory.ItemsSource = _statusHistoryView;
		((INotifyCollectionChanged)StatusHistory.Items).CollectionChanged += StatusHistory_CollectionChanged;
		if (StatusHistory.Items.Count > 0)
		{
			StatusHistory.ScrollIntoView(StatusHistory.Items[StatusHistory.Items.Count - 1]);
		}
	}

	private void StatusHistory_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
	{
		if (StatusHistory.Items.Count <= 0)
		{
			return;
		}
		DataGridColumn dataGridColumn = StatusHistory.Columns[1];
		if (dataGridColumn == null || dataGridColumn.SortDirection != ListSortDirection.Ascending)
		{
			for (int i = 0; i < StatusHistory.Columns.Count; i++)
			{
				if (StatusHistory.Columns[i].SortDirection.HasValue)
				{
					return;
				}
			}
		}
		if (VisualTreeHelper.GetChildrenCount(StatusHistory) > 0 && VisualTreeHelper.GetChild(StatusHistory, 0) is Decorator decorator)
		{
			(decorator.Child as ScrollViewer)?.ScrollToEnd();
		}
	}

	private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
	{
		if (e.Key == Key.Escape || e.Key == Key.F12)
		{
			e.Handled = true;
			Close();
		}
	}

	private void TextBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
	{
		_statusHistoryView.Refresh();
	}

	private bool AddressFilter(object item)
	{
		bool flag = false;
		StatusChangeLog statusChangeLog = item as StatusChangeLog;
		if (FilterStart.IsChecked == true && statusChangeLog.Status == ProbeStatus.Start)
		{
			flag = true;
		}
		if (FilterStop.IsChecked == true && statusChangeLog.Status == ProbeStatus.Stop)
		{
			flag = true;
		}
		if (FilterUp.IsChecked == true && statusChangeLog.Status == ProbeStatus.Up)
		{
			flag = true;
		}
		if (FilterDown.IsChecked == true && statusChangeLog.Status == ProbeStatus.Down)
		{
			flag = true;
		}
		if (flag)
		{
			string value = FilterField.Text.ToUpper();
			if (!string.IsNullOrEmpty(statusChangeLog.Alias) && statusChangeLog.Alias.ToUpper().Contains(value))
			{
				return true;
			}
			if (!string.IsNullOrEmpty(statusChangeLog.Hostname) && statusChangeLog.Hostname.ToUpper().Contains(value))
			{
				return true;
			}
			return false;
		}
		return false;
	}

	private void Filter_Click(object sender, RoutedEventArgs e)
	{
		_statusHistoryView.Refresh();
	}

	private void FilterClear_Click(object sender, RoutedEventArgs e)
	{
		FilterField.Clear();
		_statusHistoryView.Refresh();
	}

	private void Export_Click(object sender, RoutedEventArgs e)
	{
		using SaveFileDialog saveFileDialog = new SaveFileDialog();
		saveFileDialog.Title = "Export";
		saveFileDialog.RestoreDirectory = true;
		saveFileDialog.OverwritePrompt = true;
		saveFileDialog.AddExtension = true;
		saveFileDialog.AutoUpgradeEnabled = true;
		saveFileDialog.Filter = "CSV (Comma delimited)|*.csv|Text (Tab delimited)|*.txt|Text (Space delimited)|*.txt";
		saveFileDialog.FileName = "status-history.csv";
		if (saveFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK && saveFileDialog.FileName != "")
		{
			switch (saveFileDialog.FilterIndex)
			{
			case 1:
				WriteCollectionToFile(saveFileDialog.FileName, ',');
				break;
			case 2:
				WriteCollectionToFile(saveFileDialog.FileName, '\t');
				break;
			case 3:
				WriteCollectionToFile(saveFileDialog.FileName, ' ');
				break;
			}
		}
	}

	private void WriteCollectionToFile(string filePath, char delimeter)
	{
		base.Cursor = System.Windows.Input.Cursors.Wait;
		StringBuilder stringBuilder = new StringBuilder();
		foreach (StatusChangeLog item in _statusHistoryView)
		{
			stringBuilder.AppendLine(string.Format("{0}{1}{2}{3}{4}{5}{6}", item.Timestamp, delimeter, item.Hostname, delimeter, item.Alias?.Replace(",", ""), delimeter, item.StatusAsString));
		}
		try
		{
			using StreamWriter streamWriter = File.CreateText(filePath);
			streamWriter.Write(stringBuilder.ToString());
		}
		catch (Exception ex)
		{
			DialogWindow.ErrorWindow("Failed to write to '" + filePath + "'. " + ex.Message);
		}
		base.Cursor = System.Windows.Input.Cursors.Arrow;
	}

	private void Window_StateChanged(object sender, EventArgs e)
	{
		RefreshMaximizeRestoreButton();
	}

	private void OnMinimizeButtonClick(object sender, RoutedEventArgs e)
	{
		base.WindowState = WindowState.Minimized;
	}

	private void OnMaximizeRestoreButtonClick(object sender, RoutedEventArgs e)
	{
		if (base.WindowState == WindowState.Maximized)
		{
			base.WindowState = WindowState.Normal;
		}
		else
		{
			base.WindowState = WindowState.Maximized;
		}
	}

	private void OnCloseButtonClick(object sender, RoutedEventArgs e)
	{
		Close();
	}

	private void RefreshMaximizeRestoreButton()
	{
		if (base.WindowState == WindowState.Maximized)
		{
			maximizeButton.Visibility = Visibility.Collapsed;
			restoreButton.Visibility = Visibility.Visible;
		}
		else
		{
			maximizeButton.Visibility = Visibility.Visible;
			restoreButton.Visibility = Visibility.Collapsed;
		}
	}

	protected override void OnSourceInitialized(EventArgs e)
	{
		base.OnSourceInitialized(e);
		((HwndSource)PresentationSource.FromVisual(this)).AddHook(HookProc);
	}

	protected virtual IntPtr HookProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
	{
		if (msg == 36)
		{
			MINMAXINFO mINMAXINFO = (MINMAXINFO)Marshal.PtrToStructure(lParam, typeof(MINMAXINFO));
			IntPtr intPtr = MonitorFromWindow(hwnd, 2u);
			if (intPtr != IntPtr.Zero)
			{
				MONITORINFO lpmi = new MONITORINFO
				{
					cbSize = Marshal.SizeOf(typeof(MONITORINFO))
				};
				GetMonitorInfo(intPtr, ref lpmi);
				RECT rcWork = lpmi.rcWork;
				RECT rcMonitor = lpmi.rcMonitor;
				mINMAXINFO.ptMaxPosition.X = Math.Abs(rcWork.Left - rcMonitor.Left);
				mINMAXINFO.ptMaxPosition.Y = Math.Abs(rcWork.Top - rcMonitor.Top);
				mINMAXINFO.ptMaxSize.X = Math.Abs(rcWork.Right - rcWork.Left);
				mINMAXINFO.ptMaxSize.Y = Math.Abs(rcWork.Bottom - rcWork.Top);
			}
			Marshal.StructureToPtr((object)mINMAXINFO, lParam, true);
		}
		return IntPtr.Zero;
	}

	[DllImport("user32.dll")]
	private static extern IntPtr MonitorFromWindow(IntPtr handle, uint flags);

	[DllImport("user32.dll")]
	private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

	private void Window_Closed(object sender, EventArgs e)
	{
		IsWindowStateSet = true;
		switch (base.WindowState)
		{
		case WindowState.Maximized:
			_WindowState = base.WindowState;
			break;
		case WindowState.Minimized:
			_WindowState = WindowState.Normal;
			_Left = base.Left;
			_Top = base.Top;
			_Width = base.ActualWidth;
			_Height = base.ActualHeight;
			break;
		case WindowState.Normal:
			_WindowState = WindowState.Normal;
			_Left = base.Left;
			_Top = base.Top;
			_Width = base.Width;
			_Height = base.Height;
			break;
		}
	}
}
