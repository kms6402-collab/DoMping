using System;
using System.Globalization;
using System.Windows.Data;

namespace DoMping.Classes;

public class ProbeTypeToLabelConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		switch ((ProbeType)value)
		{
		case ProbeType.Ping:
			return "ICMP";
		case ProbeType.Tcp:
			return "TCP";
		case ProbeType.Service:
			return "SVC";
		case ProbeType.Dns:
			return "DNS";
		case ProbeType.Traceroute:
			return "TRACE";
		default:
			return string.Empty;
		}
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
