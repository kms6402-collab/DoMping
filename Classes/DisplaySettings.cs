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

	public event PropertyChangedEventHandler PropertyChanged;

	private void NotifyPropertyChanged(string info)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
	}
}
