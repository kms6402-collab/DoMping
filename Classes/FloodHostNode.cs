using System;
using System.ComponentModel;
using System.Threading;

namespace DoMping.Classes;

public class FloodHostNode : INotifyPropertyChanged
{
	private bool isActive;

	private long packetsPerSecond;

	private long packetsSent;

	private long packetsReceived;

	private long packetsLost;

	public DateTime StartTime { get; set; }

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
			}
		}
	}

	public long PacketsPerSecond
	{
		get
		{
			return packetsPerSecond;
		}
		set
		{
			if (value != packetsPerSecond)
			{
				packetsPerSecond = value;
				NotifyPropertyChanged("PacketsPerSecond");
			}
		}
	}

	public long PacketsSent
	{
		get
		{
			return packetsSent;
		}
		set
		{
			if (value != packetsSent)
			{
				packetsSent = value;
				NotifyPropertyChanged("PacketsSent");
			}
		}
	}

	public long PacketsReceived
	{
		get
		{
			return packetsReceived;
		}
		set
		{
			if (value != packetsReceived)
			{
				packetsReceived = value;
				NotifyPropertyChanged("PacketsReceived");
			}
		}
	}

	public long PacketsLost
	{
		get
		{
			return packetsLost;
		}
		set
		{
			if (value != packetsLost)
			{
				packetsLost = value;
				NotifyPropertyChanged("PacketsLost");
			}
		}
	}

	public string DestinationAddress { get; set; }

	public BackgroundWorker BgWorker { get; set; }

	public AutoResetEvent ResetEvent { get; set; }

	public event PropertyChangedEventHandler PropertyChanged;

	private void NotifyPropertyChanged(string info)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
	}
}
