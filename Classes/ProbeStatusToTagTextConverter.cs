using System;
using System.Globalization;
using System.Windows.Data;

namespace DoMping.Classes;

public class ProbeStatusToTagTextConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		switch ((ProbeStatus)value)
		{
		case ProbeStatus.Up:
			return "UP";
		case ProbeStatus.Down:
			return "TIMEOUT";
		case ProbeStatus.Error:
			return "DNS ERROR";
		case ProbeStatus.LatencyHigh:
		case ProbeStatus.Indeterminate:
			return "SLOW";
		case ProbeStatus.Scanner:
			return "SCANNING...";
		default:
			return "IDLE";
		}
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
