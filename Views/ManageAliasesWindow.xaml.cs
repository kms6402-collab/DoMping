using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using DoMping.Classes;
using DoMping.Properties;

namespace DoMping.Views;

public partial class ManageAliasesWindow : Window, IComponentConnector
{
	private const int GWL_STYLE = -16;

	private const int WS_MAXIMIZEBOX = 65536;

	private const int WS_MINIMIZEBOX = 131072;

	[DllImport("user32.dll")]
	private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

	[DllImport("user32.dll")]
	private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

	public ManageAliasesWindow()
	{
		InitializeComponent();
		RefreshAliasList();
	}

	public void RefreshAliasList()
	{
		AliasesDataGrid.ItemsSource = null;
		List<KeyValuePair<string, string>> list = Alias.GetAliases().ToList();
		list.Sort((KeyValuePair<string, string> pair1, KeyValuePair<string, string> pair2) => pair1.Value.CompareTo(pair2.Value));
		AliasesDataGrid.ItemsSource = list;
	}

	private void Remove_Click(object sender, RoutedEventArgs e)
	{
		if (AliasesDataGrid.SelectedIndex >= 0 && new DialogWindow(DialogWindow.DialogIcon.Warning, Strings.DialogTitle_ConfirmDelete, Strings.ManageAliases_Warn_DeleteA + " " + ((KeyValuePair<string, string>)AliasesDataGrid.SelectedItem).Value + " " + Strings.ManageAliases_Warn_DeleteB, Strings.DialogButton_Remove, isCancelButtonVisible: true)
		{
			Owner = this
		}.ShowDialog() == true)
		{
			Alias.DeleteAlias(((KeyValuePair<string, string>)AliasesDataGrid.SelectedItem).Key);
			RefreshAliasList();
		}
	}

	private void Edit_Click(object sender, RoutedEventArgs e)
	{
		if (AliasesDataGrid.SelectedIndex >= 0 && new EditAliasWindow(((KeyValuePair<string, string>)AliasesDataGrid.SelectedItem).Key, ((KeyValuePair<string, string>)AliasesDataGrid.SelectedItem).Value)
		{
			Owner = this
		}.ShowDialog() == true)
		{
			RefreshAliasList();
		}
	}

	private void New_Click(object sender, RoutedEventArgs e)
	{
		if (new NewAliasWindow
		{
			Owner = this
		}.ShowDialog() == true)
		{
			RefreshAliasList();
		}
	}

	private void Window_SourceInitialized(object sender, EventArgs e)
	{
		IntPtr handle = new WindowInteropHelper(this).Handle;
		SetWindowLong(handle, -16, GetWindowLong(handle, -16) & -65537 & -131073);
	}

	private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
	{
		if (e.Key == Key.Escape)
		{
			Close();
		}
	}
}
