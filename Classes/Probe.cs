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

	private string latencyText = string.Empty;

	private const int MaxLatencyHistoryPoints = 20;

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

	public bool HasResult => status != ProbeStatus.Inactive;

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

	public static event EventHandler ActiveCountChanged;

	public static event EventHandler AliasAutoResolved;

	public event PropertyChangedEventHandler PropertyChanged;

	private async void PerformDnsLookup(CancellationToken cancellationToken)
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
		}
		catch
		{
			if (!cancellationToken.IsCancellationRequested)
			{
				AddHistory(Environment.NewLine + "★ Unable to resolve hostname");
			}
		}
		finally
		{
			IsActive = false;
		}
	}

	private async void PerformIcmpProbe(CancellationToken cancellationToken)
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
		await TryAutoAliasFromReverseDnsAsync(Hostname);
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
					RecordLatency(0.0);
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
				RecordLatency(0.0);
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

	private async void PerformTcpProbe(CancellationToken cancellationToken)
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
		await TryAutoAliasFromReverseDnsAsync(host);
		int errorCode = 0;
		Stopwatch stopwatch = Stopwatch.StartNew();
		while (!cancellationToken.IsCancellationRequested)
		{
			stopwatch.Restart();
			using (TcpClient client = new TcpClient(ipVersion))
			{
				Statistics.Sent++;
				DisplayStatistics();
				try
				{
					IAsyncResult asyncResult = client.BeginConnect(host, portnumber, null, null);
					if (!asyncResult.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(3.0)))
					{
						throw new SocketException();
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
					RecordLatency(0.0);
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

	private async void PerformServiceProbe(CancellationToken cancellationToken)
	{
		InitializeProbe();
		string host = Hostname;
		await Application.Current.Dispatcher.BeginInvoke((Action)delegate
		{
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
		await TryAutoAliasFromReverseDnsAsync(host);
		List<(int Port, ServiceProtocol Protocol)> targets = ParseServicePorts();
		await Application.Current.Dispatcher.BeginInvoke((Action)delegate
		{
			ServicePorts = new ObservableCollection<ServicePortResult>(targets.Select((t) => new ServicePortResult
			{
				Port = t.Port,
				Protocol = t.Protocol
			}));
		});
		while (!cancellationToken.IsCancellationRequested)
		{
			Statistics.Sent++;
			DisplayStatistics();
			ServicePortCheckResult[] results = await Task.WhenAll(targets.Select((t) => CheckServicePortAsync(host, t.Port, t.Protocol, ApplicationOptions.PingTimeout)));
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
				RecordLatency(0.0);
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

	private static async Task<ServicePortCheckResult> CheckServicePortAsync(string host, int port, ServiceProtocol protocol, int timeoutMs)
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		try
		{
			if (protocol == ServiceProtocol.Tcp)
			{
				using TcpClient client = new TcpClient();
				Task connectTask = client.ConnectAsync(host, port);
				Task winner = await Task.WhenAny(connectTask, Task.Delay(timeoutMs));
				bool open = winner == connectTask && connectTask.IsCompletedSuccessfully && client.Connected;
				return new ServicePortCheckResult(port, protocol, open, stopwatch.ElapsedMilliseconds);
			}
			using UdpClient udp = new UdpClient();
			udp.Connect(host, port);
			byte[] payload = Encoding.ASCII.GetBytes("DoMping");
			await udp.SendAsync(payload, payload.Length);
			Task<UdpReceiveResult> receiveTask = udp.ReceiveAsync();
			Task winner2 = await Task.WhenAny(receiveTask, Task.Delay(timeoutMs));
			if (winner2 == receiveTask && receiveTask.IsCompletedSuccessfully)
			{
				return new ServicePortCheckResult(port, protocol, IsOpen: true, stopwatch.ElapsedMilliseconds);
			}
			return new ServicePortCheckResult(port, protocol, IsOpen: true, stopwatch.ElapsedMilliseconds);
		}
		catch (SocketException ex) when (protocol == ServiceProtocol.Udp && ex.SocketErrorCode == SocketError.ConnectionReset)
		{
			return new ServicePortCheckResult(port, protocol, IsOpen: false, stopwatch.ElapsedMilliseconds);
		}
		catch
		{
			return new ServicePortCheckResult(port, protocol, IsOpen: false, stopwatch.ElapsedMilliseconds);
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

	private async void PerformTraceroute(CancellationToken cancellationToken)
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
		await TryAutoAliasFromReverseDnsAsync(Hostname);
		AddHistory("");
		using Ping ping = new Ping();
		int ttl = 1;
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
			}
			break;
		}
		IsActive = false;
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
		CancelSource = new CancellationTokenSource();
		IsActive = true;
		if (Hostname.StartsWith("D/"))
		{
			Type = ProbeType.Dns;
			Hostname = Hostname.Substring(2);
			PerformDnsLookup(CancelSource.Token);
		}
		else if (Hostname.StartsWith("T/"))
		{
			Type = ProbeType.Traceroute;
			Hostname = Hostname.Substring(2);
			PerformTraceroute(CancelSource.Token);
		}
		else if (Hostname.StartsWith("S/"))
		{
			Type = ProbeType.Service;
			Hostname = Hostname.Substring(2);
			Task.Run(delegate
			{
				PerformServiceProbe(CancelSource.Token);
			}, CancelSource.Token);
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
			Task.Run(delegate
			{
				PerformTcpProbe(CancelSource.Token);
			}, CancelSource.Token);
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
			Task.Run(delegate
			{
				PerformIcmpProbe(CancelSource.Token);
			}, CancelSource.Token);
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
		});
	}

	private void RecordLatency(double ms)
	{
		Application.Current.Dispatcher.BeginInvoke((Action)delegate
		{
			latencyHistory.Add(ms);
			while (latencyHistory.Count > MaxLatencyHistoryPoints)
			{
				latencyHistory.RemoveAt(0);
			}
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
