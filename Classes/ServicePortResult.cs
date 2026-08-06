using System.ComponentModel;

namespace DoMping.Classes;

public enum ServiceProtocol
{
	Tcp,
	Udp
}

public record ServicePortCheckResult(int Port, ServiceProtocol Protocol, bool IsOpen, double ElapsedMs = 0.0);

public class ServicePortResult : INotifyPropertyChanged
{
	private bool? isOpen;

	public int Port { get; set; }

	public ServiceProtocol Protocol { get; set; }

	public string Label => (Protocol == ServiceProtocol.Udp) ? $"UDP {Port}" : Port.ToString();

	public bool? IsOpen
	{
		get
		{
			return isOpen;
		}
		set
		{
			if (isOpen != value)
			{
				isOpen = value;
				NotifyPropertyChanged("IsOpen");
				NotifyPropertyChanged("StatusText");
			}
		}
	}

	public string StatusText
	{
		get
		{
			if (!IsOpen.HasValue)
			{
				return "…";
			}
			return IsOpen.Value ? "OPEN" : "CLOSED";
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;

	private void NotifyPropertyChanged(string info)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
	}
}
