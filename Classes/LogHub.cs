using System;
using System.Collections.Generic;
using System.Threading.Channels;

namespace DoMping.Classes;

/// <summary>
/// One line of probe output (or a status change) as served to the web log page.
/// </summary>
public sealed class LogEntry
{
	public long Id { get; init; }

	public DateTime Time { get; init; }

	public string Host { get; init; }

	public string Alias { get; init; }

	/// <summary>"output" for a reply-log line, "status" for an UP/DOWN/... status change.</summary>
	public string Kind { get; init; }

	/// <summary>Probe status at the time the entry was recorded (ProbeStatus name).</summary>
	public string Status { get; init; }

	public string Text { get; init; }
}

/// <summary>
/// In-memory ring buffer of recent log entries plus a fan-out to live subscribers
/// (the web log page's Server-Sent Events connections). Thread-safe: Publish is
/// called from the UI thread and probe threads, subscribers read on thread-pool threads.
/// </summary>
public static class LogHub
{
	public const int Capacity = 5000;

	private static readonly object sync = new object();

	private static readonly LinkedList<LogEntry> buffer = new LinkedList<LogEntry>();

	private static readonly List<Channel<LogEntry>> subscribers = new List<Channel<LogEntry>>();

	private static long nextId = 1;

	/// <summary>Changes every time DoMping starts, so browsers can tell that entry ids were reset.</summary>
	public static readonly string SessionId = Guid.NewGuid().ToString("N");

	public static long LastId
	{
		get
		{
			lock (sync)
			{
				return nextId - 1;
			}
		}
	}

	public static void PublishOutput(Probe probe, string text)
	{
		if (probe == null || text == null)
		{
			return;
		}
		Publish(probe.Hostname, probe.Alias, "output", probe.Status.ToString(), text);
	}

	public static void PublishStatus(StatusChangeLog status)
	{
		if (status == null)
		{
			return;
		}
		Publish(status.Hostname, status.Alias, "status", status.Status.ToString(), status.StatusAsString);
	}

	private static void Publish(string host, string alias, string kind, string status, string text)
	{
		lock (sync)
		{
			LogEntry entry = new LogEntry
			{
				Id = nextId++,
				Time = DateTime.Now,
				Host = host ?? string.Empty,
				Alias = alias ?? string.Empty,
				Kind = kind,
				Status = status,
				Text = text
			};
			buffer.AddLast(entry);
			if (buffer.Count > Capacity)
			{
				buffer.RemoveFirst();
			}
			foreach (Channel<LogEntry> subscriber in subscribers)
			{
				subscriber.Writer.TryWrite(entry);
			}
		}
	}

	/// <summary>
	/// Atomically returns the buffered entries newer than <paramref name="afterId"/> and
	/// registers a channel that receives every entry published afterwards, so a client
	/// never misses or duplicates a line between backlog and live stream.
	/// </summary>
	public static Channel<LogEntry> Subscribe(long afterId, int maxBacklog, out List<LogEntry> backlog)
	{
		Channel<LogEntry> channel = Channel.CreateBounded<LogEntry>(new BoundedChannelOptions(2000)
		{
			FullMode = BoundedChannelFullMode.DropOldest,
			SingleReader = true
		});
		lock (sync)
		{
			backlog = new List<LogEntry>();
			for (LinkedListNode<LogEntry> node = buffer.Last; node != null && node.Value.Id > afterId && backlog.Count < maxBacklog; node = node.Previous)
			{
				backlog.Add(node.Value);
			}
			backlog.Reverse();
			subscribers.Add(channel);
		}
		return channel;
	}

	public static void Unsubscribe(Channel<LogEntry> channel)
	{
		lock (sync)
		{
			subscribers.Remove(channel);
		}
		channel.Writer.TryComplete();
	}

	public static int SubscriberCount
	{
		get
		{
			lock (sync)
			{
				return subscribers.Count;
			}
		}
	}
}
