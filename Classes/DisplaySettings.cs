using System.ComponentModel;

namespace DoMping.Classes;

public enum PingDisplayMode
{
	Log,
	Graph,
	Both
}

public class DisplaySettings : INotifyPropertyChanged
{
	public static DisplaySettings Instance { get; } = new DisplaySettings();

	private PingDisplayMode mode = PingDisplayMode.Both;

	private ProbeStatus? statusFilter;

	private string customFilterText = string.Empty;

	private double tileHeight = double.NaN;

	private double historyMaxHeight = 180d;

	private double sparklineHeight = 64d;

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

	public double HistoryMaxHeight
	{
		get
		{
			return historyMaxHeight;
		}
		set
		{
			if (historyMaxHeight != value)
			{
				historyMaxHeight = value;
				NotifyPropertyChanged("HistoryMaxHeight");
			}
		}
	}

	public double SparklineHeight
	{
		get
		{
			return sparklineHeight;
		}
		set
		{
			if (sparklineHeight != value)
			{
				sparklineHeight = value;
				NotifyPropertyChanged("SparklineHeight");
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
