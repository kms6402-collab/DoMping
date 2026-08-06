using System.ComponentModel;
using System.Net.NetworkInformation;

namespace DoMping.Classes;

internal class NetworkRouteNode : INotifyPropertyChanged
{
	private string hostAddress;

	private string hostName;

	private long roundTripTime;

	public string HostAddress
	{
		get
		{
			return hostAddress;
		}
		set
		{
			if (value != hostAddress)
			{
				hostAddress = value;
				NotifyPropertyChanged("HostAddress");
			}
		}
	}

	public string HostName
	{
		get
		{
			return hostName;
		}
		set
		{
			if (value != hostName)
			{
				hostName = value;
				NotifyPropertyChanged("HostName");
			}
		}
	}

	public long RoundTripTime
	{
		get
		{
			return roundTripTime;
		}
		set
		{
			if (value != roundTripTime)
			{
				roundTripTime = value;
				NotifyPropertyChanged("RoundTripTime");
			}
		}
	}

	public IPStatus ReplyStatus { get; set; }

	public int HopId { get; set; }

	public event PropertyChangedEventHandler PropertyChanged;

	private void NotifyPropertyChanged(string info)
	{
		if (this.PropertyChanged != null)
		{
			this.PropertyChanged(this, new PropertyChangedEventArgs(info));
		}
	}
}
