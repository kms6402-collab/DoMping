using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using DoMping.Classes;

namespace DoMping.Views;

public partial class TracerouteWindow : Window, IComponentConnector
{
	internal NetworkRoute Route { get; set; } = new NetworkRoute();

	public TracerouteWindow()
	{
		InitializeComponent();
		base.Topmost = ApplicationOptions.IsAlwaysOnTopEnabled;
		base.DataContext = Route;
		TraceData.ItemsSource = Route.networkRoute;
		base.Loaded += delegate
		{
			MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
		};
	}

	private void Trace_Click(object sender, RoutedEventArgs e)
	{
		if (!Route.IsActive)
		{
			if (Hostname.Text.Length != 0)
			{
				if (Route.BgWorker != null)
				{
					Route.BgWorker.CancelAsync();
				}
				TraceData.Columns[1].Width = new DataGridLength(100.0);
				TraceData.Columns[1].Width = new DataGridLength(1.0, DataGridLengthUnitType.Auto);
				TraceStatus.Text = "Tracing route...";
				TraceStatus.Visibility = Visibility.Visible;
				Route.DestinationHost = Hostname.Text;
				Route.MaxHops = 30;
				Route.PingTimeout = 2000;
				Route.networkRoute.Clear();
				Route.IsActive = true;
				Route.BgWorker = new BackgroundWorker();
				Route.ResetEvent = new AutoResetEvent(initialState: false);
				Route.BgWorker.DoWork += BackgroundThread_TraceRoute;
				Route.BgWorker.ProgressChanged += BackgroundThread_ProgressChanged;
				Route.BgWorker.RunWorkerCompleted += BgWorker_RunWorkerCompleted;
				Route.BgWorker.WorkerSupportsCancellation = true;
				Route.BgWorker.WorkerReportsProgress = true;
				Route.BgWorker.RunWorkerAsync();
			}
		}
		else
		{
			Route.BgWorker.CancelAsync();
			Route.ResetEvent.WaitOne();
			Route.IsActive = false;
			TraceStatus.Text = "• Trace cancelled";
			Hostname.Focus();
		}
	}

	private void BgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
	{
		Task.Delay(100).ContinueWith(delegate
		{
			Application.Current.Dispatcher.Invoke(delegate
			{
				Hostname.Focus();
			});
		});
	}

	public void BackgroundThread_TraceRoute(object sender, DoWorkEventArgs e)
	{
		BackgroundWorker backgroundWorker = sender as BackgroundWorker;
		byte[] bytes = Encoding.ASCII.GetBytes("https://github.com/kms6402-collab/DoMping");
		PingOptions pingOptions = new PingOptions(1, dontFragment: true);
		Stopwatch timer = new Stopwatch();
		Route.Timer = timer;
		while (!backgroundWorker.CancellationPending && Route.IsActive && pingOptions.Ttl <= Route.MaxHops)
		{
			if (IPAddress.TryParse(Route.DestinationHost, out var address))
			{
				Route.DestinationIp = address;
			}
			else
			{
				try
				{
					Route.DestinationIp = Dns.GetHostEntry(Route.DestinationHost).AddressList[0];
				}
				catch
				{
					backgroundWorker.ReportProgress(-1);
					break;
				}
			}
			using (Ping ping = new Ping())
			{
				try
				{
					Route.Timer.Reset();
					Route.Timer.Start();
					PingReply pingReply = ping.Send(Route.DestinationIp, Route.PingTimeout, bytes, pingOptions);
					if (pingReply.Status == IPStatus.TimedOut)
					{
						Route.Timer.Reset();
						Route.Timer.Start();
						pingReply = ping.Send(Route.DestinationIp, Route.PingTimeout, bytes, pingOptions);
					}
					if (!backgroundWorker.CancellationPending)
					{
						backgroundWorker.ReportProgress(pingOptions.Ttl, pingReply);
					}
					if (pingReply.Status == IPStatus.Success)
					{
						break;
					}
					Route.ResetEvent.Set();
					Thread.Sleep(100);
					int ttl = pingOptions.Ttl + 1;
					pingOptions.Ttl = ttl;
					continue;
				}
				catch
				{
				}
			}
			break;
		}
		e.Cancel = true;
		Route.ResetEvent.Set();
		Route.IsActive = false;
	}

	private void BackgroundThread_ProgressChanged(object sender, ProgressChangedEventArgs e)
	{
		Route.Timer.Stop();
		if (e.ProgressPercentage < 0)
		{
			TraceStatus.Text = "• Invalid hostname";
			return;
		}
		PingReply pingReply = e.UserState as PingReply;
		NetworkRouteNode networkRouteNode = new NetworkRouteNode();
		if (pingReply.Address != null)
		{
			networkRouteNode.HostAddress = pingReply.Address.ToString();
		}
		networkRouteNode.ReplyStatus = pingReply.Status;
		networkRouteNode.HopId = e.ProgressPercentage;
		networkRouteNode.RoundTripTime = Route.Timer.ElapsedMilliseconds;
		if (networkRouteNode.ReplyStatus == IPStatus.TimedOut)
		{
			networkRouteNode.HostAddress = "Timed out";
		}
		if (networkRouteNode.ReplyStatus == IPStatus.Success)
		{
			TraceStatus.Text = "★ Trace complete";
		}
		Route.networkRoute.Add(networkRouteNode);
		TraceData.ScrollIntoView(TraceData.Items[Route.networkRoute.Count - 1]);
	}
}
