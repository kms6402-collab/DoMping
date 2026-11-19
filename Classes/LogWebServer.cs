using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace DoMping.Classes;

/// <summary>
/// Minimal embedded HTTP server that exposes the live probe log to a browser:
///   GET /log         - the log viewer page
///   GET /log/stream  - Server-Sent Events stream of log entries (resumes via Last-Event-ID)
///   GET /log/recent  - JSON array of the buffered entries (?after=id&amp;limit=n)
/// Built on a raw TcpListener rather than HttpListener so it needs neither admin rights
/// nor a "netsh http add urlacl" reservation to listen on port 80.
/// </summary>
public static class LogWebServer
{
	private const int MaxClients = 64;

	private const int MaxRequestHeaderBytes = 16 * 1024;

	private const int DefaultBacklog = 1000;

	private static readonly object sync = new object();

	private static readonly JsonSerializerOptions jsonOptions = new JsonSerializerOptions
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase
	};

	private static TcpListener listener;

	private static CancellationTokenSource cancelSource;

	private static int clientCount;

	private static bool statusLogHooked;

	private static byte[] pageBytes;

	public static bool IsRunning { get; private set; }

	public static int Port { get; private set; }

	public static string LastError { get; private set; }

	/// <summary>Hooks the probe status-change log into the LogHub. Safe to call more than once.</summary>
	public static void Initialize()
	{
		if (statusLogHooked)
		{
			return;
		}
		statusLogHooked = true;
		Probe.StatusChangeLog.CollectionChanged += StatusChangeLog_CollectionChanged;
	}

	private static void StatusChangeLog_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
	{
		if (e.Action != NotifyCollectionChangedAction.Add || e.NewItems == null)
		{
			return;
		}
		foreach (StatusChangeLog item in e.NewItems.OfType<StatusChangeLog>())
		{
			LogHub.PublishStatus(item);
		}
	}

	/// <summary>Starts, restarts or stops the server to match the current ApplicationOptions.</summary>
	/// <returns>false if the server should be running but could not start (see LastError).</returns>
	public static bool ApplyOptions()
	{
		Initialize();
		if (!ApplicationOptions.IsWebLogEnabled)
		{
			Stop();
			LastError = null;
			return true;
		}
		if (IsRunning && Port == ApplicationOptions.WebLogPort)
		{
			return true;
		}
		Stop();
		return Start(ApplicationOptions.WebLogPort);
	}

	public static bool Start(int port)
	{
		lock (sync)
		{
			if (IsRunning)
			{
				return true;
			}
			try
			{
				// Dual-mode (IPv4 + IPv6) listener on all interfaces.
				TcpListener tcpListener = TcpListener.Create(port);
				tcpListener.Start(64);
				listener = tcpListener;
				cancelSource = new CancellationTokenSource();
				Port = port;
				IsRunning = true;
				LastError = null;
				CancellationToken token = cancelSource.Token;
				Task.Run(() => AcceptLoop(tcpListener, token));
				return true;
			}
			catch (Exception ex)
			{
				LastError = (ex is SocketException { SocketErrorCode: SocketError.AddressAlreadyInUse })
					? $"Port {port} is already in use by another program (e.g. IIS or another web server)."
					: ex.Message;
				Trace.WriteLine("Web log server failed to start: " + ex);
				IsRunning = false;
				return false;
			}
		}
	}

	public static void Stop()
	{
		lock (sync)
		{
			if (!IsRunning)
			{
				return;
			}
			IsRunning = false;
			try
			{
				cancelSource?.Cancel();
				listener?.Stop();
			}
			catch
			{
			}
			listener = null;
			cancelSource = null;
		}
	}

	/// <summary>Human-readable addresses the page can be reached at, e.g. http://192.168.10.62/log</summary>
	public static IEnumerable<string> GetUrls()
	{
		string portPart = (Port == 80) ? string.Empty : (":" + Port);
		List<string> list = new List<string>();
		try
		{
			foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
			{
				if (nic.OperationalStatus != OperationalStatus.Up || nic.NetworkInterfaceType == NetworkInterfaceType.Loopback)
				{
					continue;
				}
				foreach (UnicastIPAddressInformation addr in nic.GetIPProperties().UnicastAddresses)
				{
					if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
					{
						list.Add($"http://{addr.Address}{portPart}/log");
					}
				}
			}
		}
		catch
		{
		}
		if (list.Count == 0)
		{
			list.Add($"http://localhost{portPart}/log");
		}
		return list.Distinct();
	}

	private static async Task AcceptLoop(TcpListener tcpListener, CancellationToken token)
	{
		while (!token.IsCancellationRequested)
		{
			TcpClient client;
			try
			{
				client = await tcpListener.AcceptTcpClientAsync(token);
			}
			catch (OperationCanceledException)
			{
				break;
			}
			catch (ObjectDisposedException)
			{
				break;
			}
			catch (SocketException) when (token.IsCancellationRequested)
			{
				break;
			}
			catch (Exception ex)
			{
				Trace.WriteLine("Web log accept error: " + ex.Message);
				continue;
			}
			_ = Task.Run(() => HandleClientSafe(client, token));
		}
	}

	private static async Task HandleClientSafe(TcpClient client, CancellationToken token)
	{
		bool counted = false;
		try
		{
			client.NoDelay = true;
			using (client)
			{
				NetworkStream stream = client.GetStream();
				if (Interlocked.Increment(ref clientCount) > MaxClients)
				{
					counted = true;
					await WriteSimpleResponse(stream, "503 Service Unavailable", "text/plain; charset=utf-8", "Too many connections.", token);
					return;
				}
				counted = true;
				await HandleClient(stream, token);
			}
		}
		catch (Exception ex) when (ex is IOException || ex is SocketException || ex is OperationCanceledException || ex is ObjectDisposedException)
		{
			// Client went away or server stopped.
		}
		catch (Exception ex2)
		{
			Trace.WriteLine("Web log client error: " + ex2);
		}
		finally
		{
			if (counted)
			{
				Interlocked.Decrement(ref clientCount);
			}
		}
	}

	private static async Task HandleClient(NetworkStream stream, CancellationToken token)
	{
		string header;
		using (CancellationTokenSource readTimeout = CancellationTokenSource.CreateLinkedTokenSource(token))
		{
			readTimeout.CancelAfter(TimeSpan.FromSeconds(10));
			header = await ReadRequestHeader(stream, readTimeout.Token);
		}
		if (header == null)
		{
			return;
		}
		string[] lines = header.Split("\r\n");
		string[] requestLine = lines[0].Split(' ');
		if (requestLine.Length < 2)
		{
			await WriteSimpleResponse(stream, "400 Bad Request", "text/plain; charset=utf-8", "Bad request.", token);
			return;
		}
		string method = requestLine[0];
		string target = requestLine[1];
		Dictionary<string, string> headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		for (int i = 1; i < lines.Length; i++)
		{
			int colon = lines[i].IndexOf(':');
			if (colon > 0)
			{
				headers[lines[i].Substring(0, colon).Trim()] = lines[i].Substring(colon + 1).Trim();
			}
		}
		if (method != "GET" && method != "HEAD")
		{
			await WriteSimpleResponse(stream, "405 Method Not Allowed", "text/plain; charset=utf-8", "Method not allowed.", token, "Allow: GET, HEAD\r\n");
			return;
		}
		bool headOnly = method == "HEAD";
		string path = target;
		string query = string.Empty;
		int q = target.IndexOf('?');
		if (q >= 0)
		{
			path = target.Substring(0, q);
			query = target.Substring(q + 1);
		}
		path = path.TrimEnd('/').ToLowerInvariant();
		Dictionary<string, string> queryArgs = ParseQuery(query);
		switch (path)
		{
		case "":
			await WriteSimpleResponse(stream, "302 Found", "text/plain; charset=utf-8", "Moved to /log", token, "Location: /log\r\n", headOnly);
			break;
		case "/log":
			await WriteResponse(stream, "200 OK", "text/html; charset=utf-8", GetPageBytes(), token, null, headOnly);
			break;
		case "/log/recent":
		{
			long after = GetLong(queryArgs, "after", 0);
			int limit = (int)Math.Clamp(GetLong(queryArgs, "limit", DefaultBacklog), 1, LogHub.Capacity);
			Channel<LogEntry> channel = LogHub.Subscribe(after, limit, out List<LogEntry> backlog);
			LogHub.Unsubscribe(channel);
			byte[] json = JsonSerializer.SerializeToUtf8Bytes(backlog, jsonOptions);
			await WriteResponse(stream, "200 OK", "application/json; charset=utf-8", json, token, null, headOnly);
			break;
		}
		case "/log/stream":
			if (headOnly)
			{
				await WriteResponse(stream, "200 OK", "text/event-stream; charset=utf-8", Array.Empty<byte>(), token, null, headOnly: true);
				break;
			}
			await ServeEventStream(stream, headers, queryArgs, token);
			break;
		default:
			await WriteSimpleResponse(stream, "404 Not Found", "text/plain; charset=utf-8", "Not found. The live log is at /log", token, null, headOnly);
			break;
		}
	}

	private static async Task ServeEventStream(NetworkStream stream, Dictionary<string, string> headers, Dictionary<string, string> queryArgs, CancellationToken token)
	{
		long after = -1;
		if (headers.TryGetValue("Last-Event-ID", out string lastEventId) && long.TryParse(lastEventId, out long parsed))
		{
			after = parsed;
		}
		int limit = DefaultBacklog;
		if (after < 0)
		{
			after = GetLong(queryArgs, "after", 0);
			limit = (int)Math.Clamp(GetLong(queryArgs, "backlog", DefaultBacklog), 0, LogHub.Capacity);
		}
		else
		{
			limit = LogHub.Capacity;
		}
		if (after > LogHub.LastId)
		{
			// Ids from a previous DoMping run: start over with the normal backlog.
			after = 0;
			limit = DefaultBacklog;
		}
		Channel<LogEntry> channel = LogHub.Subscribe(after, limit, out List<LogEntry> backlog);
		try
		{
			string head = "HTTP/1.1 200 OK\r\n" +
				"Content-Type: text/event-stream; charset=utf-8\r\n" +
				"Cache-Control: no-cache, no-store\r\n" +
				"Connection: keep-alive\r\n" +
				"X-Accel-Buffering: no\r\n" +
				"Access-Control-Allow-Origin: *\r\n\r\n";
			StringBuilder sb = new StringBuilder(head);
			sb.Append("retry: 3000\n\n");
			sb.Append("event: hello\ndata: {\"session\":\"").Append(LogHub.SessionId).Append("\"}\n\n");
			foreach (LogEntry entry in backlog)
			{
				AppendEvent(sb, entry);
			}
			sb.Append("event: ready\ndata: {}\n\n");
			await WriteString(stream, sb.ToString(), token);
			ChannelReader<LogEntry> reader = channel.Reader;
			while (!token.IsCancellationRequested)
			{
				bool hasData;
				using (CancellationTokenSource heartbeat = CancellationTokenSource.CreateLinkedTokenSource(token))
				{
					heartbeat.CancelAfter(TimeSpan.FromSeconds(15));
					try
					{
						hasData = await reader.WaitToReadAsync(heartbeat.Token);
						if (!hasData)
						{
							break;
						}
					}
					catch (OperationCanceledException) when (!token.IsCancellationRequested)
					{
						// Keep-alive comment so proxies don't time out and dead clients are detected.
						await WriteString(stream, ": ping\n\n", token);
						continue;
					}
				}
				sb.Clear();
				int n = 0;
				while (n < 500 && reader.TryRead(out LogEntry entry))
				{
					AppendEvent(sb, entry);
					n++;
				}
				if (sb.Length > 0)
				{
					await WriteString(stream, sb.ToString(), token);
				}
			}
		}
		finally
		{
			LogHub.Unsubscribe(channel);
		}
	}

	private static void AppendEvent(StringBuilder sb, LogEntry entry)
	{
		sb.Append("id: ").Append(entry.Id).Append('\n');
		sb.Append("data: ").Append(JsonSerializer.Serialize(entry, jsonOptions)).Append("\n\n");
	}

	private static async Task<string> ReadRequestHeader(NetworkStream stream, CancellationToken token)
	{
		byte[] buffer = new byte[MaxRequestHeaderBytes];
		int total = 0;
		while (total < buffer.Length)
		{
			int read = await stream.ReadAsync(buffer.AsMemory(total, buffer.Length - total), token);
			if (read == 0)
			{
				return null;
			}
			total += read;
			int end = IndexOfHeaderEnd(buffer, total);
			if (end >= 0)
			{
				return Encoding.ASCII.GetString(buffer, 0, end);
			}
		}
		return null;
	}

	private static int IndexOfHeaderEnd(byte[] buffer, int length)
	{
		for (int i = 3; i < length; i++)
		{
			if (buffer[i] == '\n' && buffer[i - 1] == '\r' && buffer[i - 2] == '\n' && buffer[i - 3] == '\r')
			{
				return i - 3;
			}
		}
		return -1;
	}

	private static Task WriteSimpleResponse(NetworkStream stream, string status, string contentType, string body, CancellationToken token, string extraHeaders = null, bool headOnly = false)
	{
		return WriteResponse(stream, status, contentType, Encoding.UTF8.GetBytes(body), token, extraHeaders, headOnly);
	}

	private static async Task WriteResponse(NetworkStream stream, string status, string contentType, byte[] body, CancellationToken token, string extraHeaders = null, bool headOnly = false)
	{
		string head = $"HTTP/1.1 {status}\r\n" +
			$"Content-Type: {contentType}\r\n" +
			$"Content-Length: {body.Length}\r\n" +
			"Cache-Control: no-cache\r\n" +
			"X-Content-Type-Options: nosniff\r\n" +
			(extraHeaders ?? string.Empty) +
			"Connection: close\r\n\r\n";
		await stream.WriteAsync(Encoding.ASCII.GetBytes(head), token);
		if (!headOnly && body.Length > 0)
		{
			await stream.WriteAsync(body, token);
		}
		await stream.FlushAsync(token);
	}

	private static async Task WriteString(NetworkStream stream, string text, CancellationToken token)
	{
		await stream.WriteAsync(Encoding.UTF8.GetBytes(text), token);
		await stream.FlushAsync(token);
	}

	private static byte[] GetPageBytes()
	{
		if (pageBytes != null)
		{
			return pageBytes;
		}
		try
		{
			using Stream resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("resources/weblog.html");
			if (resource != null)
			{
				using MemoryStream ms = new MemoryStream();
				resource.CopyTo(ms);
				pageBytes = ms.ToArray();
				return pageBytes;
			}
		}
		catch (Exception ex)
		{
			Trace.WriteLine("Failed loading web log page: " + ex.Message);
		}
		return Encoding.UTF8.GetBytes("<!doctype html><title>DoMping</title><p>Log page resource missing. Raw stream: <a href=\"/log/stream\">/log/stream</a></p>");
	}

	private static Dictionary<string, string> ParseQuery(string query)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		foreach (string part in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
		{
			int eq = part.IndexOf('=');
			string key = (eq >= 0) ? part.Substring(0, eq) : part;
			string value = (eq >= 0) ? part.Substring(eq + 1) : string.Empty;
			dictionary[Uri.UnescapeDataString(key)] = Uri.UnescapeDataString(value.Replace('+', ' '));
		}
		return dictionary;
	}

	private static long GetLong(Dictionary<string, string> args, string key, long fallback)
	{
		if (args.TryGetValue(key, out string value) && long.TryParse(value, out long result))
		{
			return result;
		}
		return fallback;
	}
}
