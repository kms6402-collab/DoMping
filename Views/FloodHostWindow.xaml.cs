using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Markup;
using DoMping.Classes;

namespace DoMping.Views;

public partial class FloodHostWindow : Window, IComponentConnector
{
	private FloodHostNode _floodHost = new FloodHostNode();

	public FloodHostWindow()
	{
		InitializeComponent();
		base.Topmost = ApplicationOptions.IsAlwaysOnTopEnabled;
		base.DataContext = _floodHost;
		base.Loaded += delegate
		{
			MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
		};
	}

	private void btnFloodHost_Click(object sender, RoutedEventArgs e)
	{
		lblInformation.Visibility = Visibility.Collapsed;
		FloodHost(_floodHost);
	}

	public void FloodHost(FloodHostNode node)
	{
		if (!node.IsActive)
		{
			if (txtHostname.Text.Length != 0)
			{
				if (node.BgWorker != null)
				{
					node.BgWorker.CancelAsync();
				}
				node.DestinationAddress = txtHostname.Text;
				node.PacketsSent = 0L;
				node.PacketsReceived = 0L;
				node.PacketsLost = 0L;
				node.StartTime = DateTime.Now;
				node.IsActive = true;
				node.BgWorker = new BackgroundWorker();
				node.ResetEvent = new AutoResetEvent(initialState: false);
				node.BgWorker.DoWork += backgroundThread_FloodHost;
				node.BgWorker.WorkerSupportsCancellation = true;
				node.BgWorker.RunWorkerAsync(node);
			}
		}
		else
		{
			node.BgWorker.CancelAsync();
			node.ResetEvent.WaitOne();
			node.IsActive = false;
		}
	}

	public void backgroundThread_FloodHost(object sender, DoWorkEventArgs e)
	{
		BackgroundWorker backgroundWorker = sender as BackgroundWorker;
		FloodHostNode floodHostNode = e.Argument as FloodHostNode;
		byte[] bytes = Encoding.ASCII.GetBytes("https://github.com/kms6402-collab/DoMping");
		PingOptions options = new PingOptions(64, dontFragment: true);
		while (!backgroundWorker.CancellationPending && floodHostNode.IsActive)
		{
			using Ping ping = new Ping();
			try
			{
				long packetsSent = floodHostNode.PacketsSent + 1;
				floodHostNode.PacketsSent = packetsSent;
				if (ping.Send(floodHostNode.DestinationAddress, 100, bytes, options).Status == IPStatus.Success)
				{
					packetsSent = floodHostNode.PacketsReceived + 1;
					floodHostNode.PacketsReceived = packetsSent;
				}
				else
				{
					packetsSent = floodHostNode.PacketsLost + 1;
					floodHostNode.PacketsLost = packetsSent;
				}
				floodHostNode.ResetEvent.Set();
			}
			catch
			{
				e.Cancel = true;
				floodHostNode.ResetEvent.Set();
				floodHostNode.IsActive = false;
				return;
			}
		}
		floodHostNode.ResetEvent.Set();
	}
}
