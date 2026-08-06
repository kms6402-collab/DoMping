using System.Windows;
using System.Windows.Input;
using System.Windows.Markup;
using DoMping.Classes;
using DoMping.Properties;

namespace DoMping.Views;

public partial class EditAliasWindow : Window, IComponentConnector
{
	private string _Hostname;

	public EditAliasWindow(Probe pingItem)
		: this(pingItem.Hostname, pingItem.Alias)
	{
	}

	public EditAliasWindow(string hostname, string alias)
	{
		InitializeComponent();
		Header.Text = Strings.EditAlias_AliasFor + " " + hostname;
		MyAlias.Text = alias;
		MyAlias.SelectAll();
		_Hostname = hostname;
		base.Loaded += delegate
		{
			MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
		};
	}

	private void Save_Click(object sender, RoutedEventArgs e)
	{
		if (string.IsNullOrWhiteSpace(MyAlias.Text))
		{
			Alias.DeleteAlias(_Hostname);
		}
		else
		{
			Alias.AddAlias(_Hostname, MyAlias.Text);
		}
		base.DialogResult = true;
	}
}
