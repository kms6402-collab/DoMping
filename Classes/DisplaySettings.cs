using System.ComponentModel;

namespace DoMping.Classes;

public enum PingDisplayMode
{
	Log,
	Graph,
	Both,
	Status,
	Health,
	Heartbeat
}

public class DisplaySettings : INotifyPropertyChanged
{
	public static DisplaySettings Instance { get; } = new DisplaySettings();

	private PingDisplayMode mode = PingDisplayMode.Log;

	private ProbeStatus? statusFilter;

	private string customFilterText = string.Empty;

	private double tileHeight = double.NaN;

	public PingDisplayMode Mode
	{
		get
		{
			return mode;
		}
		set
		{
			if (mode != value)
			{
				mode = value;
				NotifyPropertyChanged("Mode");
			}
		}
	}

	public double TileHeight
	{
		get
		{
			return tileHeight;
		}
		set
		{
			if (tileHeight != value)
			{
				tileHeight = value;
				NotifyPropertyChanged("TileHeight");
			}
		}
	}

	public ProbeStatus? StatusFilter
	{
		get
		{
			return statusFilter;
		}
		set
		{
			if (statusFilter != value)
			{
				statusFilter = value;
				NotifyPropertyChanged("StatusFilter");
			}
		}
	}

	public string CustomFilterText
	{
		get
		{
			return customFilterText;
		}
		set
		{
			if (customFilterText != value)
			{
				customFilterText = value ?? string.Empty;
				NotifyPropertyChanged("CustomFilterText");
			}
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;

	private void NotifyPropertyChanged(string info)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
	}
}
