using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using DoMping.Classes;
using DoMping.Properties;

namespace DoMping.Views;

public partial class ManageFavoritesWindow : Window, IComponentConnector
{
	private Favorite SelectedFavorite;

	private const int GWL_STYLE = -16;

	private const int WS_MAXIMIZEBOX = 65536;

	private const int WS_MINIMIZEBOX = 131072;

	[DllImport("user32.dll")]
	private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

	[DllImport("user32.dll")]
	private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

	public ManageFavoritesWindow()
	{
		InitializeComponent();
		RefreshFavoriteList();
	}

	private void RefreshFavoriteList()
	{
		Favorites.ItemsSource = null;
		Favorites.Items.Clear();
		Favorites.ItemsSource = Favorite.GetTitles();
		ContentsSection.Visibility = Visibility.Collapsed;
		Contents.ItemsSource = null;
		Contents.Items.Clear();
	}

	private void Remove_Click(object sender, RoutedEventArgs e)
	{
		if (Favorites.SelectedIndex >= 0 && new DialogWindow(DialogWindow.DialogIcon.Warning, Strings.DialogTitle_ConfirmDelete, $"{Strings.ManageFavorites_Warn_DeleteA} {Favorites.SelectedItem} {Strings.ManageFavorites_Warn_DeleteB}", Strings.DialogButton_Remove, isCancelButtonVisible: true)
		{
			Owner = this
		}.ShowDialog() == true)
		{
			Favorite.Delete(Favorites.SelectedItem.ToString());
			RefreshFavoriteList();
		}
	}

	private void Favorites_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (Favorites.SelectedIndex < 0)
		{
			Grid.SetColumnSpan(Favorites, 3);
			Favorites.BorderThickness = new Thickness(1.0);
			ContentsSection.Visibility = Visibility.Collapsed;
			return;
		}
		Grid.SetColumnSpan(Favorites, 1);
		ContentsSection.Visibility = Visibility.Visible;
		SelectedFavorite = Favorite.GetContents(Favorites.SelectedItem.ToString());
		Contents.ItemsSource = null;
		Contents.Items.Clear();
		Contents.ItemsSource = SelectedFavorite.Hostnames;
		FavoriteTitle.Text = Favorites.SelectedItem.ToString();
	}

	private void Edit_Click(object sender, RoutedEventArgs e)
	{
		if (Favorites.SelectedIndex >= 0 && new NewFavoriteWindow(SelectedFavorite.Hostnames, SelectedFavorite.ColumnCount, isEditExisting: true, Favorites.SelectedItem.ToString())
		{
			Owner = this
		}.ShowDialog() == true)
		{
			RefreshFavoriteList();
		}
	}

	private void New_Click(object sender, RoutedEventArgs e)
	{
		if (new NewFavoriteWindow(new List<string>(), 2)
		{
			Owner = this
		}.ShowDialog() == true)
		{
			RefreshFavoriteList();
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
