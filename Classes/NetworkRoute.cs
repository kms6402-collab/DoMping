using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Threading;

namespace DoMping.Classes;

internal class NetworkRoute : INotifyPropertyChanged
{
	private bool isActive;

	public ObservableCollection<NetworkRouteNode> networkRoute = new ObservableCollection<NetworkRouteNode>();

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

	public string DestinationHost { get; set; }

	public IPAddress DestinationIp { get; set; }

	public int MaxHops { get; set; }

	public int PingTimeout { get; set; }

	public Stopwatch Timer { get; set; }

	public BackgroundWorker BgWorker { get; set; }

	public AutoResetEvent ResetEvent { get; set; }

	public event PropertyChangedEventHandler PropertyChanged;

	private void NotifyPropertyChanged(string info)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
	}
}
