using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Markup;
using DoMping.Classes;
using DoMping.Properties;

namespace DoMping.Views;

public partial class MainWindow : Window, IComponentConnector, IStyleConnector
{
	private ObservableCollection<Probe> _ProbeCollection = new ObservableCollection<Probe>();

	private Dictionary<string, string> _Aliases = new Dictionary<string, string>();

	private NotifyIcon NotifyIcon;

	public static RoutedCommand OptionsCommand = new RoutedCommand();

	public static RoutedCommand StartStopCommand = new RoutedCommand();

	public static RoutedCommand HelpCommand = new RoutedCommand();

	public static RoutedCommand NewInstanceCommand = new RoutedCommand();

	public static RoutedCommand TracerouteCommand = new RoutedCommand();

	public static RoutedCommand FloodHostCommand = new RoutedCommand();

	public static RoutedCommand AddProbeCommand = new RoutedCommand();

	public static RoutedCommand MultiInputCommand = new RoutedCommand();

	public static RoutedCommand StatusHistoryCommand = new RoutedCommand();

	public static RoutedCommand ViewModeCycleCommand = new RoutedCommand();

	public static RoutedCommand FilterCycleCommand = new RoutedCommand();

	public static RoutedCommand ColumnsIncreaseCommand = new RoutedCommand();

	public static RoutedCommand ColumnsDecreaseCommand = new RoutedCommand();

	public MainWindow()
	{
		InitializeComponent();
		InitializeAplication();
	}

	private void InitializeAplication()
	{
		InitializeCommandBindings();
		LoadFavorites();
		LoadAliases();
		Configuration.Load();
		RefreshGuiState();
		ProbeItemsControl.ItemsSource = _ProbeCollection;
		Probe.AliasAutoResolved += Probe_AliasAutoResolved;
		CollectionViewSource.GetDefaultView(_ProbeCollection).Filter = ProbeStatusFilter;
		DisplaySettings.Instance.PropertyChanged += DisplaySettings_PropertyChanged;
		_ProbeCollection.CollectionChanged += ProbeCollection_CollectionChanged;
	}

	private void ProbeCollection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
	{
		if (e.NewItems != null)
		{
			foreach (Probe item in e.NewItems)
			{
				item.PropertyChanged += Probe_StatusChangedForFilter;
			}
		}
		if (e.OldItems == null)
		{
			return;
		}
		foreach (Probe item2 in e.OldItems)
		{
			item2.PropertyChanged -= Probe_StatusChangedForFilter;
		}
	}

	private void DisplaySettings_PropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == "StatusFilter")
		{
			CollectionViewSource.GetDefaultView(_ProbeCollection).Refresh();
		}
	}

	private bool ProbeStatusFilter(object item)
	{
		ProbeStatus? statusFilter = DisplaySettings.Instance.StatusFilter;
		if (!statusFilter.HasValue)
		{
			return true;
		}
		Probe probe = (Probe)item;
		if (probe.Status == statusFilter.Value)
		{
			return true;
		}
		return statusFilter.Value == ProbeStatus.Indeterminate && probe.Status == ProbeStatus.LatencyHigh;
	}

	private void Probe_StatusChangedForFilter(object sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == "Status" && DisplaySettings.Instance.StatusFilter.HasValue)
		{
			CollectionViewSource.GetDefaultView(_ProbeCollection).Refresh();
		}
	}

	private void SetStatusFilter(ProbeStatus? status)
	{
		FilterAll.IsChecked = !status.HasValue;
		FilterUp.IsChecked = status == ProbeStatus.Up;
		FilterDown.IsChecked = status == ProbeStatus.Down;
		FilterSlow.IsChecked = status == ProbeStatus.Indeterminate;
		FilterScanning.IsChecked = status == ProbeStatus.Scanner;
		FilterError.IsChecked = status == ProbeStatus.Error;
		FilterIdle.IsChecked = status == ProbeStatus.Inactive;
		DisplaySettings.Instance.StatusFilter = status;
	}

	private void FilterAll_Click(object sender, RoutedEventArgs e)
	{
		SetStatusFilter(null);
	}

	private void FilterUp_Click(object sender, RoutedEventArgs e)
	{
		SetStatusFilter(ProbeStatus.Up);
	}

	private void FilterDown_Click(object sender, RoutedEventArgs e)
	{
		SetStatusFilter(ProbeStatus.Down);
	}

	private void FilterSlow_Click(object sender, RoutedEventArgs e)
	{
		SetStatusFilter(ProbeStatus.Indeterminate);
	}

	private void FilterScanning_Click(object sender, RoutedEventArgs e)
	{
		SetStatusFilter(ProbeStatus.Scanner);
	}

	private void FilterError_Click(object sender, RoutedEventArgs e)
	{
		SetStatusFilter(ProbeStatus.Error);
	}

	private void FilterIdle_Click(object sender, RoutedEventArgs e)
	{
		SetStatusFilter(ProbeStatus.Inactive);
	}

	private void SetViewMode(PingDisplayMode mode)
	{
		ViewModeList.IsChecked = mode == PingDisplayMode.Log;
		ViewModeCard.IsChecked = mode == PingDisplayMode.Graph;
		ViewModeBoth.IsChecked = mode == PingDisplayMode.Both;
		ApplicationOptions.ProbeDisplayMode = mode;
	}

	private void ViewModeList_Click(object sender, RoutedEventArgs e)
	{
		SetViewMode(PingDisplayMode.Log);
	}

	private void ViewModeCard_Click(object sender, RoutedEventArgs e)
	{
		SetViewMode(PingDisplayMode.Graph);
	}

	private void ViewModeBoth_Click(object sender, RoutedEventArgs e)
	{
		SetViewMode(PingDisplayMode.Both);
	}

	private void ViewModeCycleExecute(object sender, ExecutedRoutedEventArgs e)
	{
		PingDisplayMode mode = ApplicationOptions.ProbeDisplayMode switch
		{
			PingDisplayMode.Log => PingDisplayMode.Graph,
			PingDisplayMode.Graph => PingDisplayMode.Both,
			_ => PingDisplayMode.Log,
		};
		SetViewMode(mode);
	}

	private static readonly ProbeStatus?[] _FilterCycleOrder = new ProbeStatus?[]
	{
		null, ProbeStatus.Up, ProbeStatus.Down, ProbeStatus.Indeterminate,
		ProbeStatus.Scanner, ProbeStatus.Error, ProbeStatus.Inactive,
	};

	private void FilterCycleExecute(object sender, ExecutedRoutedEventArgs e)
	{
		int index = Array.IndexOf(_FilterCycleOrder, DisplaySettings.Instance.StatusFilter);
		ProbeStatus? next = _FilterCycleOrder[(index + 1) % _FilterCycleOrder.Length];
		SetStatusFilter(next);
	}

	private void ColumnsIncreaseExecute(object sender, ExecutedRoutedEventArgs e)
	{
		ColumnCount.Value = Math.Min(ColumnCount.Maximum, ColumnCount.Value + 1);
	}

	private void ColumnsDecreaseExecute(object sender, ExecutedRoutedEventArgs e)
	{
		ColumnCount.Value = Math.Max(ColumnCount.Minimum, ColumnCount.Value - 1);
	}

	private void Probe_AliasAutoResolved(object sender, EventArgs e)
	{
		Dispatcher.BeginInvoke((Action)LoadAliases);
	}

	private void Window_Loaded(object sender, RoutedEventArgs e)
	{
		Version version = Assembly.GetExecutingAssembly().GetName().Version;
		AppTitleText.Text = $"DOMPING · V{version.Major}.{version.Minor}.{version.Build}";
		Title = $"DoMping v{version.Major}.{version.Minor}.{version.Build}";
		ViewModeList.IsChecked = ApplicationOptions.ProbeDisplayMode == PingDisplayMode.Log;
		ViewModeCard.IsChecked = ApplicationOptions.ProbeDisplayMode == PingDisplayMode.Graph;
		ViewModeBoth.IsChecked = ApplicationOptions.ProbeDisplayMode == PingDisplayMode.Both;
		ColumnCount.Value = ((ApplicationOptions.InitialColumnCount > 0) ? ApplicationOptions.InitialColumnCount : 2);
		List<string> list = CommandLine.ParseArguments();
		if (list.Count > 0)
		{
			AddProbe(list.Count);
			for (int i = 0; i < list.Count; i++)
			{
				_ProbeCollection[i].Hostname = list[i];
				_ProbeCollection[i].Alias = (_Aliases.ContainsKey(_ProbeCollection[i].Hostname.ToLower()) ? _Aliases[_ProbeCollection[i].Hostname.ToLower()] : null);
				_ProbeCollection[i].StartStop();
			}
		}
		else
		{
			AddProbe((ApplicationOptions.InitialProbeCount > 0) ? ApplicationOptions.InitialProbeCount : 2);
			switch (ApplicationOptions.InitialStartMode)
			{
			case ApplicationOptions.StartMode.MultiInput:
				MultiInputWindowExecute(null, null);
				break;
			case ApplicationOptions.StartMode.Favorite:
				if (ApplicationOptions.InitialFavorite != null && !string.IsNullOrWhiteSpace(ApplicationOptions.InitialFavorite))
				{
					LoadFavorite(ApplicationOptions.InitialFavorite);
				}
				break;
			}
		}
		ColumnCount.Tag = ((ColumnCount.Value > (double)_ProbeCollection.Count) ? _ProbeCollection.Count : ((int)ColumnCount.Value));
	}

	private void RefreshGuiState()
	{
		PopupAlways.IsChecked = false;
		PopupNever.IsChecked = false;
		PopupWhenMinimized.IsChecked = false;
		switch (ApplicationOptions.PopupOption)
		{
		case ApplicationOptions.PopupNotificationOption.Always:
			PopupAlways.IsChecked = true;
			break;
		case ApplicationOptions.PopupNotificationOption.Never:
			PopupNever.IsChecked = true;
			break;
		case ApplicationOptions.PopupNotificationOption.WhenMinimized:
			PopupWhenMinimized.IsChecked = true;
			break;
		}
		base.Topmost = ApplicationOptions.IsAlwaysOnTopEnabled;
		if (Probe.StatusWindow != null && Probe.StatusWindow.IsLoaded)
		{
			Probe.StatusWindow.Topmost = ApplicationOptions.IsAlwaysOnTopEnabled;
		}
		if (HelpWindow._OpenWindow != null)
		{
			HelpWindow._OpenWindow.Topmost = ApplicationOptions.IsAlwaysOnTopEnabled;
		}
		foreach (Probe item in _ProbeCollection)
		{
			if (item.IsolatedWindow != null && item.IsolatedWindow.IsLoaded)
			{
				item.IsolatedWindow.Topmost = ApplicationOptions.IsAlwaysOnTopEnabled;
			}
		}
	}

	private void InitializeCommandBindings()
	{
		base.CommandBindings.Add(new CommandBinding(OptionsCommand, OptionsExecute));
		base.CommandBindings.Add(new CommandBinding(StartStopCommand, StartStopExecute));
		base.CommandBindings.Add(new CommandBinding(HelpCommand, HelpExecute));
		base.CommandBindings.Add(new CommandBinding(NewInstanceCommand, NewInstanceExecute));
		base.CommandBindings.Add(new CommandBinding(TracerouteCommand, TracerouteExecute));
		base.CommandBindings.Add(new CommandBinding(FloodHostCommand, FloodHostExecute));
		base.CommandBindings.Add(new CommandBinding(AddProbeCommand, AddProbeExecute));
		base.CommandBindings.Add(new CommandBinding(MultiInputCommand, MultiInputWindowExecute));
		base.CommandBindings.Add(new CommandBinding(StatusHistoryCommand, StatusHistoryExecute));
		base.CommandBindings.Add(new CommandBinding(ViewModeCycleCommand, ViewModeCycleExecute));
		base.CommandBindings.Add(new CommandBinding(FilterCycleCommand, FilterCycleExecute));
		base.CommandBindings.Add(new CommandBinding(ColumnsIncreaseCommand, ColumnsIncreaseExecute));
		base.CommandBindings.Add(new CommandBinding(ColumnsDecreaseCommand, ColumnsDecreaseExecute));
		base.InputBindings.Add(new InputBinding(OptionsCommand, new KeyGesture(Key.F10)));
		base.InputBindings.Add(new InputBinding(StartStopCommand, new KeyGesture(Key.F5)));
		base.InputBindings.Add(new InputBinding(HelpCommand, new KeyGesture(Key.F1)));
		base.InputBindings.Add(new InputBinding(NewInstanceCommand, new KeyGesture(Key.N, ModifierKeys.Control)));
		base.InputBindings.Add(new InputBinding(TracerouteCommand, new KeyGesture(Key.T, ModifierKeys.Control)));
		base.InputBindings.Add(new InputBinding(FloodHostCommand, new KeyGesture(Key.F, ModifierKeys.Control)));
		base.InputBindings.Add(new InputBinding(AddProbeCommand, new KeyGesture(Key.A, ModifierKeys.Control)));
		base.InputBindings.Add(new InputBinding(MultiInputCommand, new KeyGesture(Key.F2)));
		base.InputBindings.Add(new InputBinding(StatusHistoryCommand, new KeyGesture(Key.F12)));
		base.InputBindings.Add(new InputBinding(ViewModeCycleCommand, new KeyGesture(Key.F3)));
		base.InputBindings.Add(new InputBinding(FilterCycleCommand, new KeyGesture(Key.F4)));
		base.InputBindings.Add(new InputBinding(ColumnsIncreaseCommand, new KeyGesture(Key.OemPlus, ModifierKeys.Control)));
		base.InputBindings.Add(new InputBinding(ColumnsDecreaseCommand, new KeyGesture(Key.OemMinus, ModifierKeys.Control)));
		OptionsMenu.Command = OptionsCommand;
		StartStopMenu.Command = StartStopCommand;
		HelpMenu.Command = HelpCommand;
		NewInstanceMenu.Command = NewInstanceCommand;
		TracerouteMenu.Command = TracerouteCommand;
		FloodHostMenu.Command = FloodHostCommand;
		AddProbeMenu.Command = AddProbeCommand;
		MultiInputMenu.Command = MultiInputCommand;
		StatusHistoryMenu.Command = StatusHistoryCommand;
	}

	public void AddProbe(int numberOfProbes = 1)
	{
		while (numberOfProbes > 0)
		{
			_ProbeCollection.Add(new Probe());
			numberOfProbes--;
		}
	}

	public void ProbeStartStop_Click(object sender, EventArgs e)
	{
		((Probe)((System.Windows.Controls.Button)sender).DataContext).StartStop();
	}

	private void ColumnCount_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
	{
		ColumnCount.Tag = ((ColumnCount.Value > (double)_ProbeCollection.Count) ? _ProbeCollection.Count : ((int)ColumnCount.Value));
	}

	private void Hostname_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
	{
		if (e.Key == Key.Return)
		{
			Probe probe = (sender as System.Windows.Controls.TextBox).DataContext as Probe;
			probe.StartStop();
			if (_ProbeCollection.IndexOf(probe) < _ProbeCollection.Count - 1)
			{
				ContentPresenter contentPresenter = ProbeItemsControl.ItemContainerGenerator.ContainerFromIndex(_ProbeCollection.IndexOf(probe) + 1) as ContentPresenter;
				((System.Windows.Controls.TextBox)contentPresenter.ContentTemplate.FindName("Hostname", contentPresenter))?.Focus();
			}
		}
	}

	private void RemoveProbe_Click(object sender, RoutedEventArgs e)
	{
		if (_ProbeCollection.Count > 1)
		{
			Probe probe = (sender as System.Windows.Controls.Button).DataContext as Probe;
			if (probe.IsActive)
			{
				probe.StartStop();
			}
			_ProbeCollection.Remove(probe);
			ColumnCount.Tag = ((ColumnCount.Value > (double)_ProbeCollection.Count) ? _ProbeCollection.Count : ((int)ColumnCount.Value));
		}
	}

	private void MultiInputWindowExecute(object sender, ExecutedRoutedEventArgs e)
	{
		List<string> list = new List<string>();
		for (int i = 0; i < _ProbeCollection.Count; i++)
		{
			if (!string.IsNullOrWhiteSpace(_ProbeCollection[i].Hostname))
			{
				list.Add(_ProbeCollection[i].Hostname.Trim());
			}
		}
		MultiInputWindow multiInputWindow = new MultiInputWindow(list);
		multiInputWindow.Owner = this;
		if (multiInputWindow.ShowDialog() != true)
		{
			return;
		}
		RemoveAllProbes();
		List<ParsedAddress> entries = multiInputWindow.ParsedEntries;
		if (entries.Count < 1)
		{
			AddProbe();
		}
		else
		{
			AddProbe(entries.Count);
			for (int j = 0; j < entries.Count; j++)
			{
				string host = entries[j].Host;
				_ProbeCollection[j].Hostname = host;
				if (!string.IsNullOrWhiteSpace(entries[j].Alias))
				{
					string alias = entries[j].Alias.Trim();
					_ProbeCollection[j].Alias = alias;
					_Aliases[host.ToLower()] = alias;
					Alias.AddAlias(host, alias);
				}
				else
				{
					_ProbeCollection[j].Alias = (_Aliases.ContainsKey(host.ToLower()) ? _Aliases[host.ToLower()] : null);
				}
				_ProbeCollection[j].StartStop();
			}
		}
		double value = ColumnCount.Value;
		ColumnCount.Value = 1.0;
		ColumnCount.Value = value;
	}

	private void StartStopExecute(object sender, ExecutedRoutedEventArgs e)
	{
		string text = StartStopMenuHeader.Text;
		foreach (Probe item in _ProbeCollection)
		{
			if (text == Strings.Toolbar_StopAll && item.IsActive)
			{
				item.StartStop();
			}
			else if (text == Strings.Toolbar_StartAll && !item.IsActive)
			{
				item.StartStop();
			}
		}
	}

	private void HelpExecute(object sender, ExecutedRoutedEventArgs e)
	{
		if (HelpWindow._OpenWindow == null)
		{
			new HelpWindow().Show();
		}
		else
		{
			HelpWindow._OpenWindow.Activate();
		}
	}

	private void NewInstanceExecute(object sender, ExecutedRoutedEventArgs e)
	{
		try
		{
			Process process = new Process();
			process.StartInfo.FileName = Environment.ProcessPath;
			process.Start();
		}
		catch (Exception ex)
		{
			DialogWindow dialogWindow = DialogWindow.ErrorWindow(Strings.Error_FailedToLaunch + " " + ex.Message);
			dialogWindow.Owner = this;
			dialogWindow.ShowDialog();
		}
	}

	private void TracerouteExecute(object sender, ExecutedRoutedEventArgs e)
	{
		new TracerouteWindow().Show();
	}

	private void FloodHostExecute(object sender, ExecutedRoutedEventArgs e)
	{
		new FloodHostWindow().Show();
	}

	private void AddProbeExecute(object sender, ExecutedRoutedEventArgs e)
	{
		_ProbeCollection.Add(new Probe());
		ColumnCount.Tag = ((ColumnCount.Value > (double)_ProbeCollection.Count) ? _ProbeCollection.Count : ((int)ColumnCount.Value));
	}

	private void OptionsExecute(object sender, ExecutedRoutedEventArgs e)
	{
		if (new OptionsWindow
		{
			Owner = this
		}.ShowDialog() == true)
		{
			RefreshGuiState();
			RefreshProbeColors();
			ViewModeList.IsChecked = ApplicationOptions.ProbeDisplayMode == PingDisplayMode.Log;
			ViewModeCard.IsChecked = ApplicationOptions.ProbeDisplayMode == PingDisplayMode.Graph;
			ViewModeBoth.IsChecked = ApplicationOptions.ProbeDisplayMode == PingDisplayMode.Both;
		}
	}

	private void RefreshProbeColors()
	{
		for (int i = 0; i < _ProbeCollection.Count; i++)
		{
			_ProbeCollection[i].Status = _ProbeCollection[i].Status;
		}
	}

	private void RemoveAllProbes()
	{
		foreach (Probe item in _ProbeCollection)
		{
			if (item.IsActive)
			{
				item.StartStop();
			}
		}
		_ProbeCollection.Clear();
		Probe.ActiveCount = 0;
	}

	private void LoadFavorites()
	{
		for (int num = FavoritesMenu.Items.Count - 1; num > 2; num--)
		{
			FavoritesMenu.Items.RemoveAt(num);
		}
		foreach (string title in Favorite.GetTitles())
		{
			System.Windows.Controls.MenuItem menuItem = new System.Windows.Controls.MenuItem();
			menuItem.Header = title;
			menuItem.Click += delegate(object s, RoutedEventArgs r)
			{
				LoadFavorite((s as System.Windows.Controls.MenuItem).Header.ToString());
			};
			FavoritesMenu.Items.Add(menuItem);
		}
	}

	private void LoadFavorite(string favoriteTitle)
	{
		RemoveAllProbes();
		Favorite contents = Favorite.GetContents(favoriteTitle);
		if (contents.Hostnames.Count < 1)
		{
			AddProbe();
		}
		else
		{
			AddProbe(contents.Hostnames.Count);
			for (int i = 0; i < contents.Hostnames.Count; i++)
			{
				_ProbeCollection[i].Hostname = contents.Hostnames[i];
				_ProbeCollection[i].Alias = (_Aliases.ContainsKey(_ProbeCollection[i].Hostname.ToLower()) ? _Aliases[_ProbeCollection[i].Hostname.ToLower()] : null);
				_ProbeCollection[i].StartStop();
			}
		}
		ColumnCount.Value = 1.0;
		ColumnCount.Value = contents.ColumnCount;
		base.Title = favoriteTitle + " - DoMping";
	}

	private void LoadAliases()
	{
		_Aliases = Alias.GetAliases();
		List<KeyValuePair<string, string>> list = _Aliases.ToList();
		list.Sort((KeyValuePair<string, string> pair1, KeyValuePair<string, string> pair2) => pair1.Value.CompareTo(pair2.Value));
		for (int num = AliasesMenu.Items.Count - 1; num > 1; num--)
		{
			AliasesMenu.Items.RemoveAt(num);
		}
		foreach (KeyValuePair<string, string> item in list)
		{
			AliasesMenu.Items.Add(BuildAliasMenuItem(item, isContextMenu: false));
		}
		foreach (Probe item2 in _ProbeCollection)
		{
			item2.Alias = ((item2.Hostname != null && _Aliases.ContainsKey(item2.Hostname.ToLower())) ? _Aliases[item2.Hostname.ToLower()] : string.Empty);
		}
	}

	private System.Windows.Controls.MenuItem BuildAliasMenuItem(KeyValuePair<string, string> alias, bool isContextMenu)
	{
		System.Windows.Controls.MenuItem menuItem = new System.Windows.Controls.MenuItem();
		menuItem.Header = alias.Value;
		if (isContextMenu)
		{
			menuItem.Click += delegate(object s, RoutedEventArgs r)
			{
				System.Windows.Controls.MenuItem selectedMenuItem = s as System.Windows.Controls.MenuItem;
				Probe obj = (Probe)selectedMenuItem.DataContext;
				obj.Hostname = _Aliases.FirstOrDefault((KeyValuePair<string, string> x) => x.Value == selectedMenuItem.Header.ToString()).Key;
				obj.StartStop();
			};
		}
		else
		{
			menuItem.Click += delegate(object s, RoutedEventArgs r)
			{
				System.Windows.Controls.MenuItem selectedAlias = s as System.Windows.Controls.MenuItem;
				bool flag = false;
				for (int i = 0; i < _ProbeCollection.Count; i++)
				{
					if (string.IsNullOrWhiteSpace(_ProbeCollection[i].Hostname))
					{
						_ProbeCollection[i].Hostname = _Aliases.FirstOrDefault((KeyValuePair<string, string> x) => x.Value == selectedAlias.Header.ToString()).Key;
						_ProbeCollection[i].StartStop();
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					AddProbe();
					_ProbeCollection[_ProbeCollection.Count - 1].Hostname = _Aliases.FirstOrDefault((KeyValuePair<string, string> x) => x.Value == selectedAlias.Header.ToString()).Key;
					_ProbeCollection[_ProbeCollection.Count - 1].StartStop();
				}
			};
		}
		return menuItem;
	}

	private void CreateFavorite_Click(object sender, RoutedEventArgs e)
	{
		if (new NewFavoriteWindow(_ProbeCollection.Select((Probe x) => x.Hostname).ToList(), (int)ColumnCount.Value, isEditExisting: false, base.Title.EndsWith(" - DoMping") ? base.Title.Remove(base.Title.Length - " - DoMping".Length) : string.Empty)
		{
			Owner = this
		}.ShowDialog() == true)
		{
			LoadFavorites();
		}
	}

	private void ManageFavorites_Click(object sender, RoutedEventArgs e)
	{
		ManageFavoritesWindow manageFavoritesWindow = new ManageFavoritesWindow();
		manageFavoritesWindow.Owner = this;
		manageFavoritesWindow.ShowDialog();
		LoadFavorites();
	}

	private void ManageAliases_Click(object sender, RoutedEventArgs e)
	{
		ManageAliasesWindow manageAliasesWindow = new ManageAliasesWindow();
		manageAliasesWindow.Owner = this;
		manageAliasesWindow.ShowDialog();
		LoadAliases();
	}

	private void PopupAlways_Click(object sender, RoutedEventArgs e)
	{
		PopupAlways.IsChecked = true;
		PopupNever.IsChecked = false;
		PopupWhenMinimized.IsChecked = false;
		ApplicationOptions.PopupOption = ApplicationOptions.PopupNotificationOption.Always;
	}

	private void PopupNever_Click(object sender, RoutedEventArgs e)
	{
		PopupAlways.IsChecked = false;
		PopupNever.IsChecked = true;
		PopupWhenMinimized.IsChecked = false;
		ApplicationOptions.PopupOption = ApplicationOptions.PopupNotificationOption.Never;
	}

	private void PopupWhenMinimized_Click(object sender, RoutedEventArgs e)
	{
		PopupAlways.IsChecked = false;
		PopupNever.IsChecked = false;
		PopupWhenMinimized.IsChecked = true;
		ApplicationOptions.PopupOption = ApplicationOptions.PopupNotificationOption.WhenMinimized;
	}

	private void IsolatedView_Click(object sender, RoutedEventArgs e)
	{
		Probe probe = (sender as System.Windows.Controls.Button).DataContext as Probe;
		if (probe.IsolatedWindow == null || !probe.IsolatedWindow.IsLoaded)
		{
			new IsolatedPingWindow(probe).Show();
		}
		else if (probe.IsolatedWindow.IsLoaded)
		{
			probe.IsolatedWindow.Focus();
		}
	}

	private void EditAlias_Click(object sender, RoutedEventArgs e)
	{
		Probe probe = (sender as System.Windows.Controls.Button).DataContext as Probe;
		if (!string.IsNullOrEmpty(probe.Hostname))
		{
			if (_Aliases.ContainsKey(probe.Hostname.ToLower()))
			{
				probe.Alias = _Aliases[probe.Hostname.ToLower()];
			}
			else
			{
				probe.Alias = string.Empty;
			}
			if (new EditAliasWindow(probe)
			{
				Owner = this
			}.ShowDialog() == true)
			{
				LoadAliases();
			}
			Focus();
		}
	}

	private void StatusHistoryExecute(object sender, ExecutedRoutedEventArgs e)
	{
		if (Probe.StatusWindow == null || !Probe.StatusWindow.IsLoaded)
		{
			(Probe.StatusWindow = new StatusHistoryWindow(Probe.StatusChangeLog)).Show();
		}
		else if (Probe.StatusWindow.IsLoaded)
		{
			Probe.StatusWindow.Focus();
		}
	}

	private void Hostname_Loaded(object sender, RoutedEventArgs e)
	{
		for (int i = 0; i < _ProbeCollection.Count - 1; i++)
		{
			if (string.IsNullOrEmpty(_ProbeCollection[i].Hostname))
			{
				return;
			}
		}
		((System.Windows.Controls.TextBox)sender).Focus();
	}

	private void Hostname_TextChanged(object sender, TextChangedEventArgs e)
	{
		Probe probe = (sender as System.Windows.Controls.TextBox).DataContext as Probe;
		if (probe.Hostname != null)
		{
			probe.Alias = (_Aliases.ContainsKey(probe.Hostname.ToLower()) ? _Aliases[probe.Hostname.ToLower()] : null);
		}
		if (string.IsNullOrEmpty(probe.Alias))
		{
			probe.TryAutoAliasFromReverseDnsForCurrentHostname();
		}
	}

	private void Window_ContentRendered(object sender, EventArgs e)
	{
		if (_ProbeCollection.Count > 0)
		{
			ContentPresenter contentPresenter = ProbeItemsControl.ItemContainerGenerator.ContainerFromIndex(0) as ContentPresenter;
			((System.Windows.Controls.TextBox)contentPresenter.ContentTemplate.FindName("Hostname", contentPresenter))?.Focus();
		}
	}

	private void Logo_TargetUpdated(object sender, DataTransferEventArgs e)
	{
		System.Windows.Controls.Image image = sender as System.Windows.Controls.Image;
		if (image.Visibility == Visibility.Collapsed)
		{
			image.Visibility = Visibility.Collapsed;
			image.Source = null;
		}
	}

	private void ProbeTitle_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
	{
		if (e.LeftButton == MouseButtonState.Pressed)
		{
			System.Windows.DataObject dataObject = new System.Windows.DataObject();
			dataObject.SetData("Source", (sender as System.Windows.Controls.Label).DataContext as Probe);
			DragDrop.DoDragDrop(sender as DependencyObject, dataObject, System.Windows.DragDropEffects.Move);
			e.Handled = true;
		}
	}

	private void History_PreviewDragOver(object sender, System.Windows.DragEventArgs e)
	{
		e.Effects = System.Windows.DragDropEffects.Move;
		e.Handled = true;
	}

	private void Probe_Drop(object sender, System.Windows.DragEventArgs e)
	{
		if (!(e.Data.GetData("Source") is Probe item))
		{
			return;
		}
		int num;
		if (sender is System.Windows.Controls.Label)
		{
			num = _ProbeCollection.IndexOf((sender as System.Windows.Controls.Label).DataContext as Probe);
			e.Handled = true;
		}
		else
		{
			if (!(sender is DockPanel))
			{
				return;
			}
			num = _ProbeCollection.IndexOf((sender as DockPanel).DataContext as Probe);
			e.Handled = true;
		}
		int num2 = _ProbeCollection.IndexOf(item);
		if (num != num2)
		{
			_ProbeCollection.RemoveAt(num2);
			_ProbeCollection.Insert(num, item);
		}
	}

	private void HideToTray()
	{
		base.Visibility = Visibility.Hidden;
		base.WindowState = WindowState.Minimized;
		try
		{
			if (NotifyIcon == null)
			{
				ContextMenuStrip contextMenuStrip = new ContextMenuStrip();
				ToolStripMenuItem toolStripMenuItem = new ToolStripMenuItem("Options");
				toolStripMenuItem.Click += delegate
				{
					OptionsExecute(null, null);
				};
				ToolStripMenuItem toolStripMenuItem2 = new ToolStripMenuItem("Status History");
				toolStripMenuItem2.Click += delegate
				{
					StatusHistoryExecute(null, null);
				};
				ToolStripMenuItem toolStripMenuItem3 = new ToolStripMenuItem("Exit DoMping");
				toolStripMenuItem3.Click += delegate
				{
					System.Windows.Application.Current.Shutdown();
				};
				contextMenuStrip.Items.Add(toolStripMenuItem);
				contextMenuStrip.Items.Add(toolStripMenuItem2);
				contextMenuStrip.Items.Add(new ToolStripSeparator());
				contextMenuStrip.Items.Add(toolStripMenuItem3);
				NotifyIcon = new NotifyIcon
				{
					Icon = new Icon(System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/DoMping.ico")).Stream),
					Text = "DoMping",
					ContextMenuStrip = contextMenuStrip
				};
				NotifyIcon.MouseUp += NotifyIcon_MouseUp;
			}
			NotifyIcon.Visible = true;
		}
		catch
		{
			base.Visibility = Visibility.Visible;
		}
	}

	private void RestoreFromTray()
	{
		NotifyIcon.Visible = false;
		base.WindowState = WindowState.Minimized;
		base.Visibility = Visibility.Visible;
		Show();
		base.WindowState = WindowState.Normal;
	}

	private void Window_StateChanged(object sender, EventArgs e)
	{
		if (base.WindowState == WindowState.Minimized && ApplicationOptions.IsMinimizeToTrayEnabled)
		{
			HideToTray();
		}
	}

	private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
	{
		if (base.IsVisible && NotifyIcon != null && NotifyIcon.Visible)
		{
			RestoreFromTray();
		}
	}

	private void NotifyIcon_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
	{
		if (e.Button == MouseButtons.Left)
		{
			RestoreFromTray();
		}
		else if (e.Button == MouseButtons.Right)
		{
			typeof(NotifyIcon).GetMethod("ShowContextMenu", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(NotifyIcon, null);
		}
	}

	private void Window_Closing(object sender, CancelEventArgs e)
	{
		if (ApplicationOptions.IsExitToTrayEnabled)
		{
			HideToTray();
			e.Cancel = true;
		}
		else if (NotifyIcon != null)
		{
			NotifyIcon.Dispose();
		}
	}

	private void History_TextChanged(object sender, TextChangedEventArgs e)
	{
		System.Windows.Controls.TextBox textBox = sender as System.Windows.Controls.TextBox;
		textBox.SelectionStart = (textBox.DataContext as Probe).SelStart;
		textBox.SelectionLength = (textBox.DataContext as Probe).SelLength;
		if (!textBox.IsMouseCaptureWithin && textBox.SelectionLength == 0)
		{
			textBox.ScrollToEnd();
		}
	}

	private void History_SelectionChanged(object sender, RoutedEventArgs e)
	{
		System.Windows.Controls.TextBox textBox = sender as System.Windows.Controls.TextBox;
		(textBox.DataContext as Probe).SelStart = textBox.SelectionStart;
		(textBox.DataContext as Probe).SelLength = textBox.SelectionLength;
	}
}
