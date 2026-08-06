using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using DoMping.Classes;
using DoMping.Properties;

namespace DoMping.Views;

public partial class NewFavoriteWindow : Window, IComponentConnector
{
	private List<string> HostList;

	private int ColumnCount;

	private bool IsExisting;

	private string OriginalTitle;

	private const int GWL_STYLE = -16;

	private const int WS_MAXIMIZEBOX = 65536;

	private const int WS_MINIMIZEBOX = 131072;

	[DllImport("user32.dll")]
	private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

	[DllImport("user32.dll")]
	private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

	public NewFavoriteWindow(List<string> hostList, int columnCount, bool isEditExisting = false, string title = "")
	{
		InitializeComponent();
		HostList = hostList;
		ColumnCount = columnCount;
		IsExisting = isEditExisting;
		OriginalTitle = title;
		MyHosts.Text = string.Join(Environment.NewLine, hostList).Trim();
		MyColumnCount.Text = columnCount.ToString();
		MyTitle.Text = title;
		if (isEditExisting)
		{
			base.Title = "Edit Favorite";
			Header.Text = "Edit an existing favorite";
			HeaderIcon.Source = (DrawingImage)Application.Current.Resources["icon.edit"];
		}
		base.Loaded += delegate
		{
			MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
		};
	}

	private void Save_Click(object sender, RoutedEventArgs e)
	{
		if (!int.TryParse(MyColumnCount.Text, out ColumnCount) || ColumnCount < 1 || ColumnCount > 10)
		{
			DialogWindow dialogWindow = DialogWindow.ErrorWindow("Please enter a valid number of columns (between 1 and 10).");
			dialogWindow.Owner = this;
			dialogWindow.ShowDialog();
			MyColumnCount.Focus();
			MyColumnCount.SelectAll();
			return;
		}
		if (Favorite.IsTitleInvalid(MyTitle.Text))
		{
			DialogWindow dialogWindow2 = DialogWindow.ErrorWindow(Strings.NewFavorite_Error_InvalidName);
			dialogWindow2.Owner = this;
			dialogWindow2.ShowDialog();
			MyTitle.Focus();
			MyTitle.SelectAll();
			return;
		}
		HostList = (from host in MyHosts.Text.Trim().Split(',', '\n')
			select host.Trim()).ToList();
		if (HostList.All((string x) => string.IsNullOrWhiteSpace(x)))
		{
			DialogWindow dialogWindow3 = DialogWindow.ErrorWindow("You have not entered any hosts. Provide at least one host for this favorite set.");
			dialogWindow3.Owner = this;
			dialogWindow3.ShowDialog();
			MyHosts.Focus();
			MyHosts.SelectAll();
		}
		else if ((!IsExisting && Favorite.TitleExists(MyTitle.Text)) || (IsExisting && !string.Equals(OriginalTitle, MyTitle.Text) && Favorite.TitleExists(MyTitle.Text)))
		{
			DialogWindow dialogWindow4 = DialogWindow.WarningWindow(MyTitle.Text + " " + Strings.NewFavorite_Warn_AlreadyExists, Strings.DialogButton_Overwrite);
			dialogWindow4.Owner = this;
			if (dialogWindow4.ShowDialog() == true)
			{
				SaveFavorite();
			}
		}
		else
		{
			SaveFavorite();
		}
	}

	private void SaveFavorite()
	{
		if (IsExisting && !MyTitle.Text.Equals(OriginalTitle))
		{
			Favorite.Rename(OriginalTitle, MyTitle.Text);
		}
		Favorite.Save(MyTitle.Text, HostList, ColumnCount);
		base.DialogResult = true;
	}

	private void MyColumnCount_PreviewTextInput(object sender, TextCompositionEventArgs e)
	{
		if (new Regex("[^0-9.-]+").IsMatch(e.Text))
		{
			e.Handled = true;
		}
	}

	private void Window_SourceInitialized(object sender, EventArgs e)
	{
		HideMinimizeAndMaximizeButtons();
	}

	protected void HideMinimizeAndMaximizeButtons()
	{
		IntPtr handle = new WindowInteropHelper(this).Handle;
		SetWindowLong(handle, -16, GetWindowLong(handle, -16) & -65537 & -131073);
	}

	private void MyHosts_Drop(object sender, DragEventArgs e)
	{
		if (!e.Data.GetDataPresent(DataFormats.FileDrop))
		{
			return;
		}
		try
		{
			string[] obj = (string[])e.Data.GetData(DataFormats.FileDrop);
			if (new FileInfo(obj[0]).Length > 10240)
			{
				throw new FileFormatException();
			}
			IEnumerable<string> source = new List<string>(File.ReadAllLines(obj[0])).Where((string x) => !string.IsNullOrWhiteSpace(x) && (char.IsLetterOrDigit(x[0]) || x[0] == '['));
			MyHosts.Text = string.Join(Environment.NewLine, source.Select((string x) => x.Trim()));
		}
		catch (FileFormatException)
		{
			DialogWindow dialogWindow = DialogWindow.ErrorWindow($"The file is too large and cannot be opened. The maximum file size is {10L} kb.");
			dialogWindow.Owner = this;
			dialogWindow.ShowDialog();
		}
		catch
		{
			DialogWindow dialogWindow2 = DialogWindow.ErrorWindow("File could not be opened. Make sure the file is a plain text file.");
			dialogWindow2.Owner = this;
			dialogWindow2.ShowDialog();
		}
	}

	private void MyHosts_PreviewDragOver(object sender, DragEventArgs e)
	{
		e.Effects = DragDropEffects.Copy;
		e.Handled = true;
	}
}
