using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using DoMping.Properties;
using DoMping.Views;

namespace DoMping.Classes;

public class Probe : INotifyPropertyChanged
{
	public static ObservableCollection<StatusChangeLog> StatusChangeLog = new ObservableCollection<StatusChangeLog>();

	public static StatusHistoryWindow StatusWindow;

	private static Mutex mutex = new Mutex();

	private static int activeCount;

	private ObservableCollection<string> history;

	private ProbeType type = ProbeType.Ping;

	private string hostname;

	private string alias;

	private ProbeStatus status = ProbeStatus.Inactive;

	private bool isActive;

	private string statisticsText;

	private ObservableCollection<ServicePortResult> servicePorts = new ObservableCollection<ServicePortResult>();

	private ObservableCollection<double> latencyHistory = new ObservableCollection<double>();

	private ObservableCollection<bool> replyHistory = new ObservableCollection<bool>();

	private string heartbeatText = string.Empty;

	private string latencyText = string.Empty;

	private double uptimePercent;

	private string uptimeText = "-";

	private string latencySummaryText = string.Empty;

	private const int MaxLatencyHistoryPoints = 20;

	// D/, T/ or S/ prefix stripped from Hostname by the last StartStop, and the bare host
	// it applied to (see HostnameWithPrefix).
	private string typePrefix;

	private string typePrefixHost;

	// First address IsHostInvalid resolved for the current probe run; reused so the
	// service port scan doesn't do a fresh DNS lookup for every port on every cycle.
	private IPAddress resolvedAddress;

	// UDP gives no answer for an open (or filtered) port; we only wait long enough to
	// catch an ICMP "port unreachable", which arrives within one round trip.
	private const int MaxUdpWaitMs = 1000;

	public static int ActiveCount
	{
		get
		{
			return activeCount;
		}
		set
		{
			activeCount = value;
			OnActiveCountChanged(EventArgs.Empty);
		}
	}

	public IsolatedPingWindow IsolatedWindow { get; set; }

	public int IndeterminateCount { get; set; }

	public PingStatistics Statistics { get; set; } = new PingStatistics();

	public int SelStart { get; set; }

	public int SelLength { get; set; }

	public CancellationTokenSource CancelSource { get; set; }

	public ObservableCollection<string> History
	{
		get
		{
			return history;
		}
		set
		{
			history = value;
			history.CollectionChanged += History_CollectionChanged;
			NotifyPropertyChanged("History");
		}
	}

	public string HistoryAsString
	{
		get
		{
			if (History == null)
			{
				return string.Empty;
			}
			return string.Join(Environment.NewLine, History);
		}
	}

	public ProbeType Type
	{
		get
		{
			return type;
		}
		set
		{
			type = value;
			NotifyPropertyChanged("Type");
		}
	}

	public string Hostname
	{
		get
		{
			return hostname;
		}
		set
		{
			if (value != hostname)
			{
				hostname = value.Trim();
				NotifyPropertyChanged("Hostname");
			}
		}
	}

	public string Alias
	{
		get
		{
			return alias;
		}
		set
		{
			alias = value;
			NotifyPropertyChanged("Alias");
		}
	}

	public ProbeStatus Status
	{
		get
		{
			return status;
		}
		set
		{
			status = value;
			NotifyPropertyChanged("Status");
			NotifyPropertyChanged("HasResult");
		}
	}

	// A finished DNS lookup / traceroute goes back to IDLE but must keep its output on
	// screen, so "has something to show" is not simply "status != Inactive".
	public bool HasResult => status != ProbeStatus.Inactive || showFinishedResult;

	private bool showFinishedResult;

	private bool ShowFinishedResult
	{
		get
		{
			return showFinishedResult;
		}
		set
		{
			if (value != showFinishedResult)
			{
				showFinishedResult = value;
				NotifyPropertyChanged("HasResult");
			}
		}
	}

	// One-shot probes (DNS, traceroute): back to IDLE (or ERROR) once done, keeping
	// the results visible. Previously the status stayed SCANNING forever.
	private void FinishOneShotProbe(ProbeStatus finalStatus, CancellationToken cancellationToken)
	{
		if (cancellationToken.IsCancellationRequested)
		{
			// Stopped by the user: StopProbe has already set the state.
			return;
		}
		ShowFinishedResult = true;
		Status = finalStatus;
		IsActive = false;
	}

	public bool IsActive
	{
		get
		{
			return isActive;
		}
		set
		{
			if (value != isActive)
			{
				isActive = value;
				NotifyPropertyChanged("IsActive");
				mutex.WaitOne();
				if (value)
				{
					ActiveCount++;
				}
				else
				{
					ActiveCount--;
				}
				mutex.ReleaseMutex();
				NotifyPropertyChanged("NumberOfActivePings");
			}
		}
	}

	public ObservableCollection<ServicePortResult> ServicePorts
	{
		get
		{
			return servicePorts;
		}
		set
		{
			servicePorts = value;
			NotifyPropertyChanged("ServicePorts");
		}
	}

	public ObservableCollection<double> LatencyHistory
	{
		get
		{
			return latencyHistory;
		}
		set
		{
			latencyHistory = value;
			NotifyPropertyChanged("LatencyHistory");
		}
	}

	public string LatencyText
	{
		get
		{
			return latencyText;
		}
		set
		{
			if (value != latencyText)
			{
				latencyText = value;
				NotifyPropertyChanged("LatencyText");
			}
		}
	}

	public string StatisticsText
	{
		get
		{
			return statisticsText;
		}
		set
		{
			if (value != statisticsText)
			{
				statisticsText = value;
				NotifyPropertyChanged("StatisticsText");
			}
		}
	}

	// success/failure of each recent reply, kept in step with LatencyHistory (Heartbeat view)
	public ObservableCollection<bool> ReplyHistory
	{
		get
		{
			return replyHistory;
		}
		set
		{
			replyHistory = value;
			NotifyPropertyChanged("ReplyHistory");
		}
	}

	public string HeartbeatText
	{
		get
		{
			return heartbeatText;
		}
		set
		{
			if (value != heartbeatText)
			{
				heartbeatText = value;
				NotifyPropertyChanged("HeartbeatText");
			}
		}
	}

	public double UptimePercent
	{
		get
		{
			return uptimePercent;
		}
		set
		{
			if (value != uptimePercent)
			{
				uptimePercent = value;
				NotifyPropertyChanged("UptimePercent");
			}
		}
	}

	public string UptimeText
	{
		get
		{
			return uptimeText;
		}
		set
		{
			if (value != uptimeText)
			{
				uptimeText = value;
				NotifyPropertyChanged("UptimeText");
			}
		}
	}

	public string LatencySummaryText
	{
		get
		{
			return latencySummaryText;
		}
		set
		{
			if (value != latencySummaryText)
			{
				latencySummaryText = value;
				NotifyPropertyChanged("LatencySummaryText");
			}
		}
	}

	public static event EventHandler ActiveCountChanged;

	public static event EventHandler AliasAutoResolved;

	public event PropertyChangedEventHandler PropertyChanged;

	private async Task PerformDnsLookup(CancellationToken cancellationToken)
	{
		IsActive = true;
		History = new ObservableCollection<string>();
		Status = ProbeStatus.Scanner;
		try
		{
			AddHistory("[•] Resolving " + Hostname + ":" + Environment.NewLine);
			switch (Uri.CheckHostName(Hostname))
			{
			case UriHostNameType.IPv4:
			case UriHostNameType.IPv6:
			{
				IPHostEntry iPHostEntry = await Dns.GetHostEntryAsync(Hostname);
				cancellationToken.ThrowIfCancellationRequested();
				if (iPHostEntry != null)
				{
					AddHistory("    " + iPHostEntry.HostName);
				}
				break;
			}
			case UriHostNameType.Dns:
			{
				IPAddress[] obj = await Dns.GetHostAddressesAsync(Hostname);
				cancellationToken.ThrowIfCancellationRequested();
				IPAddress[] array = obj;
				foreach (IPAddress arg in array)
				{
					AddHistory($"    {arg}");
				}
				break;
			}
			default:
				throw new Exception();
			}
			AddHistory(Environment.NewLine + Environment.NewLine + "★ Done");
			FinishOneShotProbe(ProbeStatus.Inactive, cancellationToken);
		}
		catch
		{
			if (!cancellationToken.IsCancellationRequested)
			{
				AddHistory(Environment.NewLine + "★ Unable to resolve hostname");
			}
			FinishOneShotProbe(ProbeStatus.Error, cancellationToken);
		}
		finally
		{
			IsActive = false;
		}
	}

	private async Task PerformIcmpProbe(CancellationToken cancellationToken)
	{
		InitializeProbe();
		await Application.Current.Dispatcher.BeginInvoke((Action)delegate
		{
			AddHistory("*** Pinging " + Hostname + ":");
		});
		if (await IsHostInvalid(Hostname, cancellationToken))
		{
			if (!cancellationToken.IsCancellationRequested)
			{
				StopProbe(ProbeStatus.Error);
			}
			return;
		}
		// Reverse DNS can take several seconds for hosts without a PTR record; run it in
		// the background so the first ping / port check is not held up waiting for it.
		_ = TryAutoAliasFromReverseDnsAsync(Hostname);
		using Ping ping = new Ping();
		while (!cancellationToken.IsCancellationRequested)
		{
			try
			{
				Statistics.Sent++;
				PingReply pingReply = await ping.SendPingAsync(Hostname, ApplicationOptions.PingTimeout, ApplicationOptions.Buffer, ApplicationOptions.GetPingOptions);
				if (cancellationToken.IsCancellationRequested)
				{
					return;
				}
				if (pingReply.Status == IPStatus.Success)
				{
					Statistics.Received++;
					IndeterminateCount = 0;
					LatencyText = (pingReply.RoundtripTime < 1) ? "<1ms" : $"{pingReply.RoundtripTime}ms";
					RecordLatency(pingReply.RoundtripTime);
					if (Status == ProbeStatus.Down)
					{
						TriggerStatusChange(new StatusChangeLog
						{
							Timestamp = DateTime.Now,
							Hostname = Hostname,
							Alias = Alias,
							Status = ProbeStatus.Up
						});
						if (ApplicationOptions.IsEmailAlertEnabled)
						{
							Util.SendEmail("up", Hostname, Alias);
						}
					}
					Status = ProbeStatus.Up;
				}
				else
				{
					Statistics.Lost++;
					IndeterminateCount++;
					LatencyText = string.Empty;
					RecordLatency(0.0, success: false);
					if (Status == ProbeStatus.Up)
					{
						Status = ProbeStatus.Indeterminate;
					}
					if (Status == ProbeStatus.Scanner)
					{
						Status = ProbeStatus.Down;
					}
					if (Status == ProbeStatus.Indeterminate && IndeterminateCount >= ApplicationOptions.AlertThreshold)
					{
						Status = ProbeStatus.Down;
						TriggerStatusChange(new StatusChangeLog
						{
							Timestamp = DateTime.Now,
							Hostname = Hostname,
							Alias = Alias,
							Status = ProbeStatus.Down
						});
						if (ApplicationOptions.IsEmailAlertEnabled)
						{
							Util.SendEmail("down", Hostname, Alias);
						}
					}
				}
				if (!cancellationToken.IsCancellationRequested)
				{
					DisplayStatistics();
					DisplayIcmpReply(pingReply);
					await IcmpWait(pingReply.Status);
				}
			}
			catch (Exception ex)
			{
				Statistics.Lost++;
				LatencyText = string.Empty;
				RecordLatency(0.0, success: false);
				if (Status == ProbeStatus.Scanner)
				{
					Status = ProbeStatus.Down;
				}
				if (Status != ProbeStatus.Down)
				{
					Status = ProbeStatus.Down;
					TriggerStatusChange(new StatusChangeLog
					{
						Timestamp = DateTime.Now,
						Hostname = Hostname,
						Alias = Alias,
						Status = ProbeStatus.Down
					});
					if (ApplicationOptions.IsEmailAlertEnabled)
					{
						Util.SendEmail("error", Hostname, Alias);
					}
				}
				DisplayStatistics();
				DisplayIcmpReply(null, ex);
				await Task.Delay(ApplicationOptions.PingInterval);
			}
		}
	}

	private async Task IcmpWait(IPStatus ipStatus)
	{
		if (ipStatus == IPStatus.TimedOut)
		{
			if (ApplicationOptions.PingInterval > ApplicationOptions.PingTimeout)
			{
				await Task.Delay(ApplicationOptions.PingInterval - ApplicationOptions.PingTimeout);
			}
			else
			{
				await Task.Delay(1000);
			}
		}
		else
		{
			await Task.Delay(ApplicationOptions.PingInterval);
		}
	}

	private void DisplayIcmpReply(PingReply pingReply, Exception ex = null)
	{
		if (pingReply == null && ex == null)
		{
			return;
		}
		StringBuilder pingOutput = new StringBuilder("[" + DateTime.Now.ToLongTimeString() + "]  ");
		if (pingReply != null)
		{
			switch (pingReply.Status)
			{
			case IPStatus.Success:
				pingOutput.Append("Reply from ");
				pingOutput.Append(pingReply.Address.ToString());
				if (pingReply.RoundtripTime < 1)
				{
					pingOutput.Append("  [<1ms]");
				}
				else
				{
					pingOutput.Append($"  [{pingReply.RoundtripTime} ms]");
				}
				break;
			case IPStatus.DestinationHostUnreachable:
				pingOutput.Append("Reply  [Host unreachable]");
				break;
			case IPStatus.DestinationNetworkUnreachable:
				pingOutput.Append("Reply  [Network unreachable]");
				break;
			case IPStatus.DestinationUnreachable:
				pingOutput.Append("Reply  [Unreachable]");
				break;
			case IPStatus.TimedOut:
				pingOutput.Append("Request timed out.");
				break;
			default:
				pingOutput.Append(pingReply.Status.ToString());
				break;
			}
		}
		else if (ex.InnerException is SocketException)
		{
			pingOutput.Append("Unable to resolve hostname.");
		}
		else
		{
			pingOutput.Append(ex.Message);
		}
		Application.Current.Dispatcher.BeginInvoke((Action)delegate
		{
			AddHistory(pingOutput.ToString());
		});
		WriteToLog(pingOutput.ToString());
	}

	private async Task PerformTcpProbe(CancellationToken cancellationToken)
	{
		InitializeProbe();
		string host = Hostname.Substring(0, Hostname.LastIndexOf(':'));
		host = host.Trim('[', ']');
		string s = Hostname.Substring(Hostname.LastIndexOf(':') + 1);
		bool isPortOpen = false;
		if (!int.TryParse(s, out var portnumber) || portnumber < 1 || portnumber > 65535)
		{
			await Application.Current.Dispatcher.BeginInvoke((Action)delegate
			{
				AddHistory("Invalid port number.");
			});
			StopProbe(ProbeStatus.Error);
			return;
		}
		await Application.Current.Dispatcher.BeginInvoke((Action)delegate
		{
			AddHistory($"*** Pinging {host} on port {portnumber}:");
		});
		if (await IsHostInvalid(host, cancellationToken))
		{
			if (!cancellationToken.IsCancellationRequested)
			{
				StopProbe(ProbeStatus.Error);
			}
			return;
		}
		AddressFamily ipVersion = AddressFamily.InterNetwork;
		try
		{
			switch (Uri.CheckHostName(host))
			{
			case UriHostNameType.IPv4:
				ipVersion = AddressFamily.InterNetwork;
				break;
			case UriHostNameType.IPv6:
				ipVersion = AddressFamily.InterNetworkV6;
				break;
			case UriHostNameType.Dns:
			{
				IPAddress[] array = await Dns.GetHostAddressesAsync(host);
				cancellationToken.ThrowIfCancellationRequested();
				host = array[0].ToString();
				ipVersion = array[0].AddressFamily;
				break;
			}
			}
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			await Application.Current.Dispatcher.BeginInvoke((Action)delegate
			{
				AddHistory("Error: " + ex2.Message);
			});
			StopProbe(ProbeStatus.Error);
			return;
		}
		// Reverse DNS can take several seconds for hosts without a PTR record; run it in
		// the background so the first ping / port check is not held up waiting for it.
		_ = TryAutoAliasFromReverseDnsAsync(host);
		int errorCode = 0;
		Stopwatch stopwatch = Stopwatch.StartNew();
		while (!cancellationToken.IsCancellationRequested)
		{
			stopwatch.Restart();
			using (TcpClient client = new TcpClient(ipVersion))
			{
				DisableSynRetries(client.Client);
				Statistics.Sent++;
				DisplayStatistics();
				try
				{
					IAsyncResult asyncResult = client.BeginConnect(host, portnumber, null, null);
					if (!asyncResult.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(3.0)))
					{
						// An explicit error code: the parameterless constructor picks up whatever
						// the last Win32 error happened to be (11001 there would wrongly stop the
						// probe with "Unable to resolve hostname").
						throw new SocketException((int)SocketError.TimedOut);
					}
					client.EndConnect(asyncResult);
					if (cancellationToken.IsCancellationRequested)
					{
						break;
					}
					if (Status == ProbeStatus.Down)
					{
						TriggerStatusChange(new StatusChangeLog
						{
							Timestamp = DateTime.Now,
							Hostname = Hostname,
							Alias = Alias,
							Status = ProbeStatus.Up
						});
						if (ApplicationOptions.IsEmailAlertEnabled)
						{
							Util.SendEmail("up", Hostname, Alias);
						}
					}
					Statistics.Received++;
					IndeterminateCount = 0;
					Status = ProbeStatus.Up;
					isPortOpen = true;
					long connectElapsed = stopwatch.ElapsedMilliseconds;
					LatencyText = (connectElapsed < 1) ? "<1ms" : $"{connectElapsed}ms";
					RecordLatency(connectElapsed);
				}
				catch (SocketException ex3)
				{
					stopwatch.Stop();
					LatencyText = string.Empty;
					RecordLatency(0.0, success: false);
					if (cancellationToken.IsCancellationRequested)
					{
						break;
					}
					if (Status == ProbeStatus.Up)
					{
						Status = ProbeStatus.Indeterminate;
					}
					if (Status == ProbeStatus.Scanner)
					{
						Status = ProbeStatus.Down;
					}
					IndeterminateCount++;
					if (Status == ProbeStatus.Indeterminate && IndeterminateCount >= ApplicationOptions.AlertThreshold)
					{
						Status = ProbeStatus.Down;
						TriggerStatusChange(new StatusChangeLog
						{
							Timestamp = DateTime.Now,
							Hostname = Hostname,
							Alias = Alias,
							Status = ProbeStatus.Down
						});
						if (ApplicationOptions.IsEmailAlertEnabled)
						{
							Util.SendEmail("down", Hostname, Alias);
						}
					}
					if (ex3.ErrorCode == 11001)
					{
						await Application.Current.Dispatcher.BeginInvoke((Action)delegate
						{
							AddHistory("Unable to resolve hostname.");
						});
						StopProbe(ProbeStatus.Error);
						break;
					}
					Statistics.Lost++;
					isPortOpen = false;
					errorCode = ex3.ErrorCode;
				}
				catch (Exception ex4)
				{
					Exception ex5 = ex4;
					await Application.Current.Dispatcher.BeginInvoke((Action)delegate
					{
						AddHistory("Error: " + ex5.Message);
					});
					StopProbe(ProbeStatus.Error);
					break;
				}
				client.Close();
			}
			if (!cancellationToken.IsCancellationRequested)
			{
				DisplayTcpReply(isPortOpen, portnumber, errorCode, stopwatch.ElapsedMilliseconds);
				DisplayStatistics();
				await Task.Delay(ApplicationOptions.PingInterval);
			}
		}
	}

	private void DisplayTcpReply(bool isPortOpen, int portnumber, int errorCode, long elapsedTime)
	{
		StringBuilder pingOutput = new StringBuilder($"[{DateTime.Now.ToLongTimeString()}]  {Strings.Probe_Port} {portnumber}: ");
		if (isPortOpen)
		{
			pingOutput.Append($"{Strings.Probe_PortOpen}  [{elapsedTime}{Strings.Milliseconds_Symbol}]");
		}
		else
		{
			pingOutput.Append(Strings.Probe_PortClosed);
		}
		Application.Current.Dispatcher.BeginInvoke((Action)delegate
		{
			AddHistory(pingOutput.ToString());
		});
		WriteToLog(pingOutput.ToString());
	}

	private async Task PerformServiceProbe(CancellationToken cancellationToken)
	{
		InitializeProbe();
		string host = Hostname;
		List<(int Port, ServiceProtocol Protocol)> targets = ParseServicePorts();
		// Show the port rows ("…" = pending) right away instead of after DNS resolution.
		ObservableCollection<ServicePortResult> portRows = new ObservableCollection<ServicePortResult>(targets.Select((t) => new ServicePortResult
		{
			Port = t.Port,
			Protocol = t.Protocol
		}));
		await Application.Current.Dispatcher.BeginInvoke((Action)delegate
		{
			ServicePorts = portRows;
			AddHistory("*** Checking services on " + host + ":");
		});
		if (await IsHostInvalid(host, cancellationToken))
		{
			if (!cancellationToken.IsCancellationRequested)
			{
				StopProbe(ProbeStatus.Error);
			}
			return;
		}
		// Reverse DNS can take several seconds for hosts without a PTR record; run it in
		// the background so the first ping / port check is not held up waiting for it.
		_ = TryAutoAliasFromReverseDnsAsync(host);
		// Connect to the address resolved once above, not the hostname: otherwise every
		// port of every cycle performs its own DNS lookup before it even starts connecting.
		IPAddress targetAddress = resolvedAddress;
		while (!cancellationToken.IsCancellationRequested)
		{
			Statistics.Sent++;
			DisplayStatistics();
			ServicePortCheckResult[] results = await Task.WhenAll(targets.Select(async delegate((int Port, ServiceProtocol Protocol) t, int index)
			{
				ServicePortCheckResult r0 = await CheckServicePortAsync(host, targetAddress, t.Port, t.Protocol, ApplicationOptions.PingTimeout, cancellationToken);
				// Update each port's OPEN/CLOSED as soon as its own check finishes, rather
				// than waiting for the slowest port (a filtered port takes the full timeout).
				if (!cancellationToken.IsCancellationRequested)
				{
					_ = Application.Current.Dispatcher.BeginInvoke((Action)delegate
					{
						if (index < portRows.Count)
						{
							portRows[index].IsOpen = r0.IsOpen;
						}
					});
				}
				return r0;
			}));
			if (cancellationToken.IsCancellationRequested)
			{
				break;
			}
			int openCount = results.Count((ServicePortCheckResult r) => r.IsOpen);
			bool allOpen = openCount == results.Length;
			if (openCount > 0)
			{
				double avgMs = results.Where((ServicePortCheckResult r) => r.IsOpen).Average((ServicePortCheckResult r) => r.ElapsedMs);
				LatencyText = (avgMs < 1.0) ? "<1ms" : $"{Math.Round(avgMs)}ms";
				RecordLatency(avgMs);
			}
			else
			{
				LatencyText = string.Empty;
				RecordLatency(0.0, success: false);
			}
			await Application.Current.Dispatcher.BeginInvoke((Action)delegate
			{
				for (int i = 0; i < results.Length; i++)
				{
					if (i < ServicePorts.Count)
					{
						ServicePorts[i].IsOpen = results[i].IsOpen;
					}
					DisplayServiceReply(results[i]);
				}
			});
			foreach (ServicePortCheckResult r2 in results)
			{
				WriteToLog(FormatServiceReply(r2));
			}
			if (allOpen)
			{
				Statistics.Received++;
				IndeterminateCount = 0;
				if (Status == ProbeStatus.Down)
				{
					TriggerStatusChange(new StatusChangeLog
					{
						Timestamp = DateTime.Now,
						Hostname = Hostname,
						Alias = Alias,
						Status = ProbeStatus.Up
					});
					if (ApplicationOptions.IsEmailAlertEnabled)
					{
						Util.SendEmail("up", Hostname, Alias);
					}
				}
				Status = ProbeStatus.Up;
			}
			else
			{
				Statistics.Lost++;
				IndeterminateCount++;
				if (Status == ProbeStatus.Up)
				{
					Status = ProbeStatus.Indeterminate;
				}
				if (Status == ProbeStatus.Scanner)
				{
					Status = ProbeStatus.Down;
				}
				if (Status == ProbeStatus.Indeterminate && IndeterminateCount >= ApplicationOptions.AlertThreshold)
				{
					Status = ProbeStatus.Down;
					TriggerStatusChange(new StatusChangeLog
					{
						Timestamp = DateTime.Now,
						Hostname = Hostname,
						Alias = Alias,
						Status = ProbeStatus.Down
					});
					if (ApplicationOptions.IsEmailAlertEnabled)
					{
						Util.SendEmail("down", Hostname, Alias);
					}
				}
			}
			if (!cancellationToken.IsCancellationRequested)
			{
				DisplayStatistics();
				await Task.Delay(ApplicationOptions.PingInterval);
			}
		}
	}

	private static List<(int Port, ServiceProtocol Protocol)> ParseServicePorts()
	{
		List<(int, ServiceProtocol)> list = new List<(int, ServiceProtocol)>();
		string[] array = (ApplicationOptions.ServiceTcpPorts ?? string.Empty).Split(',');
		foreach (string raw in array)
		{
			if (int.TryParse(raw.Trim(), out var p) && p > 0 && p <= 65535)
			{
				list.Add((p, ServiceProtocol.Tcp));
			}
		}
		array = (ApplicationOptions.ServiceUdpPorts ?? string.Empty).Split(',');
		foreach (string raw2 in array)
		{
			if (int.TryParse(raw2.Trim(), out var p2) && p2 > 0 && p2 <= 65535)
			{
				list.Add((p2, ServiceProtocol.Udp));
			}
		}
		if (list.Count == 0)
		{
			list.Add((80, ServiceProtocol.Tcp));
		}
		return list;
	}

	private static async Task<ServicePortCheckResult> CheckServicePortAsync(string host, IPAddress address, int port, ServiceProtocol protocol, int timeoutMs, CancellationToken cancellationToken)
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		try
		{
			if (protocol == ServiceProtocol.Tcp)
			{
				using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
				timeout.CancelAfter(timeoutMs);
				using TcpClient client = (address != null) ? new TcpClient(address.AddressFamily) : new TcpClient();
				DisableSynRetries(client.Client);
				// Cancelling the token aborts the pending connect, so a filtered port no
				// longer leaves a half-open socket attempt running after we gave up on it.
				if (address != null)
				{
					await client.ConnectAsync(address, port, timeout.Token);
				}
				else
				{
					await client.ConnectAsync(host, port, timeout.Token);
				}
				return new ServicePortCheckResult(port, protocol, client.Connected, stopwatch.ElapsedMilliseconds);
			}
			using UdpClient udp = (address != null) ? new UdpClient(address.AddressFamily) : new UdpClient();
			if (address != null)
			{
				udp.Connect(address, port);
			}
			else
			{
				udp.Connect(host, port);
			}
			byte[] payload = Encoding.ASCII.GetBytes("DoMping");
			await udp.SendAsync(payload, payload.Length);
			using CancellationTokenSource udpWait = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			udpWait.CancelAfter(Math.Min(timeoutMs, MaxUdpWaitMs));
			// A reply means open. An ICMP "port unreachable" surfaces as ConnectionReset
			// from ReceiveAsync (caught below) and means closed. No answer at all within the
			// wait means open|filtered, which is reported as open. (Previously the receive
			// task's ConnectionReset was never observed, so closed UDP ports showed OPEN.)
			await udp.ReceiveAsync(udpWait.Token);
			return new ServicePortCheckResult(port, protocol, IsOpen: true, stopwatch.ElapsedMilliseconds);
		}
		catch (OperationCanceledException) when (protocol == ServiceProtocol.Udp && !cancellationToken.IsCancellationRequested)
		{
			return new ServicePortCheckResult(port, protocol, IsOpen: true, stopwatch.ElapsedMilliseconds);
		}
		catch
		{
			return new ServicePortCheckResult(port, protocol, IsOpen: false, stopwatch.ElapsedMilliseconds);
		}
	}

	// Windows answers a "connection refused" (RST) by re-sending the SYN twice, so a
	// closed port took ~2 s - the whole timeout - to report CLOSED. TCP_NOSYNRETRIES
	// makes the RST fail the connect immediately (measured: 2000 ms -> 1-7 ms). Open
	// ports are unaffected, and with our 2 s timeout a lost SYN was never retried in
	// time anyway (the first SYN retransmit comes after ~3 s).
	private const SocketOptionName TcpNoSynRetries = (SocketOptionName)9;

	private static void DisableSynRetries(Socket socket)
	{
		try
		{
			socket.SetSocketOption(SocketOptionLevel.Tcp, TcpNoSynRetries, optionValue: true);
		}
		catch (SocketException)
		{
			// Not supported on this Windows build: keep the default behavior.
		}
	}

	private static string FormatServiceReply(ServicePortCheckResult result)
	{
		string label = (result.Protocol == ServiceProtocol.Udp) ? $"UDP {result.Port}" : $"TCP {result.Port}";
		string state = result.IsOpen ? Strings.Probe_PortOpen : Strings.Probe_PortClosed;
		return $"[{DateTime.Now.ToLongTimeString()}]  {label}: {state}";
	}

	private void DisplayServiceReply(ServicePortCheckResult result)
	{
		AddHistory(FormatServiceReply(result));
	}

	private async Task PerformTraceroute(CancellationToken cancellationToken)
	{
		IsActive = true;
		History = new ObservableCollection<string>();
		Status = ProbeStatus.Scanner;
		AddHistory("[•] Tracing route to " + Hostname + ":");
		if (await IsHostInvalid(Hostname, cancellationToken))
		{
			if (!cancellationToken.IsCancellationRequested)
			{
				StopProbe(ProbeStatus.Error);
			}
			return;
		}
		// Reverse DNS can take several seconds for hosts without a PTR record; run it in
		// the background so the first ping / port check is not held up waiting for it.
		_ = TryAutoAliasFromReverseDnsAsync(Hostname);
		AddHistory("");
		using Ping ping = new Ping();
		int ttl = 1;
		bool traceFailed = false;
		Stopwatch timer = new Stopwatch();
		while (!cancellationToken.IsCancellationRequested && ttl <= 30)
		{
			try
			{
				timer.Restart();
				PingReply pingReply = await ping.SendPingAsync(Hostname, 2000, Encoding.ASCII.GetBytes("https://github.com/kms6402-collab/DoMping"), new PingOptions
				{
					Ttl = ttl
				});
				timer.Stop();
				string arg = string.Empty;
				if (pingReply.Address != null)
				{
					arg = pingReply.Address.ToString();
				}
				if (cancellationToken.IsCancellationRequested)
				{
					break;
				}
				if (pingReply.Status != IPStatus.TtlExpired && pingReply.Status != IPStatus.Success)
				{
					AddHistory($"{ttl,2}   {pingReply.Status}");
				}
				else
				{
					AddHistory(string.Format("{0,2}   {1,-15}   [{2} ms]", ttl, arg, (timer.ElapsedMilliseconds < 1) ? "<1" : timer.ElapsedMilliseconds.ToString()));
				}
				if (pingReply.Status == IPStatus.Success)
				{
					AddHistory(Environment.NewLine + "★ Trace complete");
					break;
				}
				ttl++;
				if (ttl > 30)
				{
					AddHistory(Environment.NewLine + "★ Trace complete");
				}
				await Task.Delay(100);
				continue;
			}
			catch (Exception ex)
			{
				AddHistory(ex.Message);
				traceFailed = true;
			}
			break;
		}
		FinishOneShotProbe(traceFailed ? ProbeStatus.Error : ProbeStatus.Inactive, cancellationToken);
		IsActive = false;
	}

	public static bool HasTypePrefix(string host)
	{
		return host != null && (host.StartsWith("D/") || host.StartsWith("T/") || host.StartsWith("S/"));
	}

	public static string StripTypePrefix(string host)
	{
		return HasTypePrefix(host) ? host.Substring(2) : host;
	}

	// The hostname as the user originally typed it, including a D/, T/ or S/ prefix.
	// StartStop strips the prefix from Hostname (so aliases, logs and the tile show the
	// bare host), so anything that saves or re-lists the probe (favorites, multi-input)
	// must use this instead of Hostname or the probe silently turns into a plain ping.
	public string HostnameWithPrefix
	{
		get
		{
			if (typePrefix != null && !string.IsNullOrEmpty(Hostname) && !HasTypePrefix(Hostname) && string.Equals(Hostname, typePrefixHost, StringComparison.OrdinalIgnoreCase))
			{
				return typePrefix + Hostname;
			}
			return Hostname;
		}
	}

	public void StartStop()
	{
		if (string.IsNullOrEmpty(Hostname))
		{
			return;
		}
		if (IsActive)
		{
			StopProbe(ProbeStatus.Inactive);
			return;
		}
		// Restarting a stopped S/ (or D/, T/) probe whose hostname wasn't edited must
		// run the same kind of probe again, not fall back to an ICMP ping.
		string target = HostnameWithPrefix;
		CancelSource = new CancellationTokenSource();
		// Captured now: the probe loops must keep the token of THIS run. Reading
		// CancelSource.Token later (inside Task.Run) could pick up the token of a newer
		// run after a quick stop/start, leaving two loops running for one tile.
		CancellationToken token = CancelSource.Token;
		ShowFinishedResult = false;
		IsActive = true;
		if (HasTypePrefix(target))
		{
			typePrefix = target.Substring(0, 2);
			typePrefixHost = target.Substring(2);
			Hostname = typePrefixHost;
		}
		else
		{
			typePrefix = null;
			typePrefixHost = null;
		}
		if (target.StartsWith("D/"))
		{
			Type = ProbeType.Dns;
			_ = GuardProbe(() => PerformDnsLookup(token));
		}
		else if (target.StartsWith("T/"))
		{
			Type = ProbeType.Traceroute;
			_ = GuardProbe(() => PerformTraceroute(token));
		}
		else if (target.StartsWith("S/"))
		{
			Type = ProbeType.Service;
			Task.Run(() => GuardProbe(() => PerformServiceProbe(token)), token);
			Application.Current.Dispatcher.BeginInvoke((Action)delegate
			{
				mutex.WaitOne();
				StatusChangeLog.Add(new StatusChangeLog
				{
					Timestamp = DateTime.Now,
					Hostname = Hostname,
					Alias = Alias,
					Status = ProbeStatus.Start
				});
				mutex.ReleaseMutex();
			});
		}
		else if (Hostname.Count((char f) => f == ':') == 1 || Hostname.Contains("]:"))
		{
			Type = ProbeType.Tcp;
			Task.Run(() => GuardProbe(() => PerformTcpProbe(token)), token);
			Application.Current.Dispatcher.BeginInvoke((Action)delegate
			{
				mutex.WaitOne();
				StatusChangeLog.Add(new StatusChangeLog
				{
					Timestamp = DateTime.Now,
					Hostname = Hostname,
					Alias = Alias,
					Status = ProbeStatus.Start
				});
				mutex.ReleaseMutex();
			});
		}
		else
		{
			Type = ProbeType.Ping;
			Task.Run(() => GuardProbe(() => PerformIcmpProbe(token)), token);
			Application.Current.Dispatcher.BeginInvoke((Action)delegate
			{
				mutex.WaitOne();
				StatusChangeLog.Add(new StatusChangeLog
				{
					Timestamp = DateTime.Now,
					Hostname = Hostname,
					Alias = Alias,
					Status = ProbeStatus.Start
				});
				mutex.ReleaseMutex();
			});
		}
	}

	// Runs one probe loop so that an unexpected exception can never take the whole
	// application down. The loops used to be "async void": any exception escaping them
	// was rethrown on the thread pool and terminated DoMping (seen as a
	// NullReferenceException in PerformTcpProbe). Now the tile turns ERROR, the error
	// is shown in its log, and the full stack trace goes to error.log for diagnosis.
	private async Task GuardProbe(Func<Task> probe)
	{
		try
		{
			await probe();
		}
		catch (OperationCanceledException)
		{
		}
		catch (Exception ex)
		{
			ReportProbeFailure(ex);
		}
	}

	private void ReportProbeFailure(Exception ex)
	{
		try
		{
			Util.WriteErrorLog("Probe '" + Hostname + "' (" + Type + ") failed", ex);
			System.Windows.Threading.Dispatcher dispatcher = Application.Current?.Dispatcher;
			if (dispatcher == null || dispatcher.HasShutdownStarted)
			{
				return;
			}
			dispatcher.BeginInvoke((Action)delegate
			{
				try
				{
					AddHistory("Error: " + ex.Message);
					if (IsActive)
					{
						StopProbe(ProbeStatus.Error);
					}
					else
					{
						Status = ProbeStatus.Error;
					}
				}
				catch
				{
				}
			});
		}
		catch
		{
		}
	}

	private void InitializeProbe()
	{
		IsActive = true;
		Status = ProbeStatus.Scanner;
		Statistics.Reset();
		History = new ObservableCollection<string>();
		LatencyText = string.Empty;
		Application.Current.Dispatcher.BeginInvoke((Action)delegate
		{
			LatencyHistory = new ObservableCollection<double>();
			ReplyHistory = new ObservableCollection<bool>();
			LatencySummaryText = string.Empty;
			HeartbeatText = string.Empty;
		});
	}

	private void RecordLatency(double ms, bool success = true)
	{
		Application.Current.Dispatcher.BeginInvoke((Action)delegate
		{
			latencyHistory.Add(ms);
			replyHistory.Add(success);
			while (latencyHistory.Count > MaxLatencyHistoryPoints)
			{
				latencyHistory.RemoveAt(0);
			}
			while (replyHistory.Count > MaxLatencyHistoryPoints)
			{
				replyHistory.RemoveAt(0);
			}
			List<double> replies = latencyHistory.Where((double x, int i) => replyHistory[i]).ToList();
			LatencySummaryText = (replies.Count == 0) ? "no replies" : $"min {replies.Min():0} · avg {replies.Average():0} · max {replies.Max():0} ms";
			HeartbeatText = $"last {replyHistory.Count} · {replyHistory.Count((bool x) => !x)} failed";
		});
	}

	private void StopProbe(ProbeStatus status)
	{
		CancelSource.Cancel();
		Status = status;
		IsActive = false;
		Application.Current.Dispatcher.BeginInvoke((Action)delegate
		{
			mutex.WaitOne();
			if (status != ProbeStatus.Error)
			{
				WriteFinalStatisticsToHistory();
			}
			StatusChangeLog.Add(new StatusChangeLog
			{
				Timestamp = DateTime.Now,
				Hostname = Hostname,
				Alias = Alias,
				Status = ProbeStatus.Stop
			});
			mutex.ReleaseMutex();
		});
	}

	private async Task<bool> IsHostInvalid(string host, CancellationToken cancellationToken)
	{
		bool result = default(bool);
		int num;
		resolvedAddress = null;
		try
		{
			switch (Uri.CheckHostName(host))
			{
			case UriHostNameType.Dns:
			{
				IPAddress[] ipAddresses = await Dns.GetHostAddressesAsync(host);
				cancellationToken.ThrowIfCancellationRequested();
				if (ipAddresses.Length != 0)
				{
					resolvedAddress = ipAddresses[0];
					await Application.Current.Dispatcher.BeginInvoke((Action)delegate
					{
						AddHistory($"    ({ipAddresses[0]})");
					});
				}
				break;
			}
			default:
				throw new Exception();
			case UriHostNameType.IPv4:
			case UriHostNameType.IPv6:
				IPAddress.TryParse(host.Trim('[', ']'), out resolvedAddress);
				break;
			}
			result = false;
			return result;
		}
		catch
		{
			num = 1;
		}
		if (num != 1)
		{
			return result;
		}
		if (!cancellationToken.IsCancellationRequested)
		{
			await Application.Current.Dispatcher.BeginInvoke((Action)delegate
			{
				AddHistory(Environment.NewLine + "Unable to resolve hostname");
			});
		}
		return true;
	}

	public void TryAutoAliasFromReverseDnsForCurrentHostname()
	{
		if (IsActive || string.IsNullOrEmpty(Hostname))
		{
			return;
		}
		_ = TryAutoAliasFromReverseDnsAsync(Hostname);
	}

	private async Task TryAutoAliasFromReverseDnsAsync(string host)
	{
		if (!string.IsNullOrEmpty(Alias))
		{
			return;
		}
		UriHostNameType hostNameType = Uri.CheckHostName(host);
		if (hostNameType != UriHostNameType.IPv4 && hostNameType != UriHostNameType.IPv6)
		{
			return;
		}
		try
		{
			IPHostEntry entry = await Dns.GetHostEntryAsync(host);
			if (entry != null && !string.IsNullOrWhiteSpace(entry.HostName) && !entry.HostName.Equals(host, StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(Alias))
			{
				string resolvedAlias = entry.HostName;
				await Application.Current.Dispatcher.BeginInvoke((Action)delegate
				{
					if (string.IsNullOrEmpty(Alias))
					{
						Alias = resolvedAlias;
					}
				});
				global::DoMping.Classes.Alias.AddAlias(host, resolvedAlias);
				AliasAutoResolved?.Invoke(null, EventArgs.Empty);
			}
		}
		catch
		{
		}
	}

	private void WriteToLog(string message)
	{
		if (!ApplicationOptions.IsLogOutputEnabled || ApplicationOptions.LogPath.Length <= 0)
		{
			return;
		}
		string path = ApplicationOptions.LogPath + "\\" + Util.GetSafeFilename(Hostname) + ".txt";
		try
		{
			using StreamWriter streamWriter = new StreamWriter(path, append: true);
			streamWriter.WriteLine(message.Insert(1, DateTime.Now.ToShortDateString() + " "));
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			Exception ex3 = ex2;
			ApplicationOptions.IsLogOutputEnabled = false;
			Application.Current.Dispatcher.BeginInvoke((Action)delegate
			{
				DialogWindow.ErrorWindow("Failed writing to log file. Logging has been disabled. " + ex3.Message).ShowDialog();
			});
		}
	}

	private void WriteToStatusChangesLog(StatusChangeLog status)
	{
		if (!ApplicationOptions.IsLogStatusChangesEnabled || ApplicationOptions.LogStatusChangesPath.Length <= 0)
		{
			return;
		}
		try
		{
			using StreamWriter streamWriter = new StreamWriter(ApplicationOptions.LogStatusChangesPath, append: true);
			streamWriter.WriteLine(DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToLongTimeString() + "\t" + status.Hostname + "\t" + status.Alias + "\t" + status.StatusAsString);
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			Exception ex3 = ex2;
			ApplicationOptions.IsLogStatusChangesEnabled = false;
			Application.Current.Dispatcher.BeginInvoke((Action)delegate
			{
				DialogWindow.ErrorWindow("Failed writing to log file. Logging has been disabled. " + ex3.Message).ShowDialog();
			});
		}
	}

	private void DisplayStatistics()
	{
		StatisticsText = $"Sent: {Statistics.Sent} Received: {Statistics.Received} Lost: {Statistics.Lost}";
		uint sent = Statistics.Sent;
		UptimePercent = (sent == 0) ? 0.0 : (100.0 * Statistics.Received / sent);
		UptimeText = (sent == 0) ? "-" : $"{UptimePercent:0.0}%";
	}

	private void TriggerStatusChange(StatusChangeLog status)
	{
		Application.Current.Dispatcher.BeginInvoke((Action)delegate
		{
			if (ApplicationOptions.PopupOption == ApplicationOptions.PopupNotificationOption.Always || (ApplicationOptions.PopupOption == ApplicationOptions.PopupNotificationOption.WhenMinimized && Application.Current.MainWindow.WindowState == WindowState.Minimized))
			{
				mutex.WaitOne();
				if (!Application.Current.Windows.OfType<PopupNotificationWindow>().Any())
				{
					for (int i = 0; i < StatusChangeLog.Count; i++)
					{
						StatusChangeLog[i].HasStatusBeenCleared = true;
					}
				}
				StatusChangeLog.Add(status);
				mutex.ReleaseMutex();
				if (StatusWindow != null && StatusWindow.IsLoaded)
				{
					if (StatusWindow.WindowState == WindowState.Minimized)
					{
						StatusWindow.WindowState = WindowState.Normal;
					}
					StatusWindow.Focus();
				}
				else if (!Application.Current.Windows.OfType<PopupNotificationWindow>().Any())
				{
					new PopupNotificationWindow(StatusChangeLog).Show();
				}
			}
			else
			{
				mutex.WaitOne();
				StatusChangeLog.Add(status);
				mutex.ReleaseMutex();
			}
		});
		if (ApplicationOptions.IsLogStatusChangesEnabled && ApplicationOptions.LogStatusChangesPath.Length > 0)
		{
			mutex.WaitOne();
			WriteToStatusChangesLog(status);
			mutex.ReleaseMutex();
		}
		if (ApplicationOptions.IsAudioDownAlertEnabled && status.Status == ProbeStatus.Down)
		{
			try
			{
				using SoundPlayer soundPlayer = new SoundPlayer(ApplicationOptions.AudioDownFilePath);
				soundPlayer.Play();
				return;
			}
			catch (Exception ex)
			{
				Exception ex2 = ex;
				Exception ex3 = ex2;
				ApplicationOptions.IsAudioDownAlertEnabled = false;
				Application.Current.Dispatcher.BeginInvoke((Action)delegate
				{
					DialogWindow.ErrorWindow("Failed to play audio file. Audio alerts have been disabled. " + ex3.Message).ShowDialog();
				});
				return;
			}
		}
		if (!ApplicationOptions.IsAudioUpAlertEnabled || status.Status != ProbeStatus.Up)
		{
			return;
		}
		try
		{
			using SoundPlayer soundPlayer2 = new SoundPlayer(ApplicationOptions.AudioUpFilePath);
			soundPlayer2.Play();
		}
		catch (Exception ex4)
		{
			Exception ex2 = ex4;
			Exception ex5 = ex2;
			ApplicationOptions.IsAudioUpAlertEnabled = false;
			Application.Current.Dispatcher.BeginInvoke((Action)delegate
			{
				DialogWindow.ErrorWindow("Failed to play audio file. Audio alerts have been disabled. " + ex5.Message).ShowDialog();
			});
		}
	}

	protected static void OnActiveCountChanged(EventArgs e)
	{
		Probe.ActiveCountChanged?.Invoke(null, e);
	}

	private void History_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
	{
		NotifyPropertyChanged("HistoryAsString");
	}

	public void AddHistory(string historyItem)
	{
		History.Add(historyItem);
		if (History.Count > 3600)
		{
			History.RemoveAt(0);
		}
	}

	public void WriteFinalStatisticsToHistory()
	{
		if (Statistics == null || Statistics.Sent == 0)
		{
			return;
		}
		List<int> list = new List<int>();
		Regex regex = new Regex("  \\[(?<rtt><?\\d+) ?" + Strings.Milliseconds_Symbol + "]$");
		foreach (string item in History)
		{
			Match match = regex.Match(item);
			if (match.Success)
			{
				if (match.Groups["rtt"].Value == "<1")
				{
					list.Add(0);
				}
				else
				{
					list.Add(int.Parse(match.Groups["rtt"].Value));
				}
			}
		}
		AddHistory("");
		AddHistory($"Sent {Statistics.Sent}, " + $"Received {Statistics.Received}, " + $"Lost {Statistics.Sent - Statistics.Received} ({100 * (Statistics.Sent - Statistics.Received) / Statistics.Sent}% loss)");
		if (list.Count > 0)
		{
			AddHistory($"Minimum ({list.Min()}{Strings.Milliseconds_Symbol}), " + $"Maximum ({list.Max()}{Strings.Milliseconds_Symbol}), " + "Average (" + list.Average().ToString("0.##") + Strings.Milliseconds_Symbol + ")");
		}
		AddHistory(" ");
	}

	private void NotifyPropertyChanged(string info)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
	}
}
