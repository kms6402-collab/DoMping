using System;
using System.Windows;
using System.Windows.Markup;
using DoMping.Classes;

namespace DoMping.Views;

public partial class NewConfigurationWindow : Window, IComponentConnector
{
	public NewConfigurationWindow()
	{
		InitializeComponent();
		FilePath.Text = Configuration.FilePath;
	}

	private void Save_Click(object sender, RoutedEventArgs e)
	{
		if (PortableMode.IsChecked == true)
		{
			Configuration.FilePath = AppDomain.CurrentDomain.BaseDirectory + "DoMping.xml";
		}
		base.DialogResult = true;
	}

	private void PortableMode_Click(object sender, RoutedEventArgs e)
	{
		if (PortableMode.IsChecked == true)
		{
			FilePath.Text = AppDomain.CurrentDomain.BaseDirectory + "DoMping.xml";
		}
		else
		{
			FilePath.Text = Configuration.FilePath;
		}
	}
}
