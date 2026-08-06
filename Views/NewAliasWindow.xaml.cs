using System.Windows;
using System.Windows.Input;
using System.Windows.Markup;
using DoMping.Classes;
using DoMping.Properties;

namespace DoMping.Views;

public partial class NewAliasWindow : Window, IComponentConnector
{
	public NewAliasWindow()
	{
		InitializeComponent();
		base.Loaded += delegate
		{
			MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
		};
	}

	private void Save_Click(object sender, RoutedEventArgs e)
	{
		if (Alias.IsHostInvalid(MyHost.Text))
		{
			DialogWindow dialogWindow = DialogWindow.ErrorWindow(Strings.NewAlias_Error_InvalidHost);
			dialogWindow.Owner = this;
			dialogWindow.ShowDialog();
			MyHost.Focus();
			MyHost.SelectAll();
		}
		else if (Alias.IsNameInvalid(MyAlias.Text))
		{
			DialogWindow dialogWindow2 = DialogWindow.ErrorWindow(Strings.NewAlias_Error_InvalidAlias);
			dialogWindow2.Owner = this;
			dialogWindow2.ShowDialog();
			MyAlias.Focus();
			MyAlias.SelectAll();
		}
		else
		{
			Alias.AddAlias(MyHost.Text, MyAlias.Text);
			base.DialogResult = true;
		}
	}
}
