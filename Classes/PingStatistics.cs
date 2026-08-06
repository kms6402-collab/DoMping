using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DoMping.Classes;

public class PingStatistics : INotifyPropertyChanged
{
	private uint sent;

	private uint received;

	private uint lost;

	private uint error;

	public uint Sent
	{
		get
		{
			return sent;
		}
		set
		{
			sent = value;
			OnPropertyChanged("Sent");
		}
	}

	public uint Received
	{
		get
		{
			return received;
		}
		set
		{
			received = value;
			OnPropertyChanged("Received");
		}
	}

	public uint Lost
	{
		get
		{
			return lost;
		}
		set
		{
			lost = value;
			OnPropertyChanged("Lost");
		}
	}

	public uint Error
	{
		get
		{
			return error;
		}
		set
		{
			error = value;
			OnPropertyChanged("Error");
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;

	public void Reset()
	{
		sent = 0u;
		received = 0u;
		lost = 0u;
		error = 0u;
		OnPropertyChanged("Sent");
		OnPropertyChanged("Received");
		OnPropertyChanged("Lost");
		OnPropertyChanged("Error");
	}

	protected void OnPropertyChanged([CallerMemberName] string name = null)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
	}
}
