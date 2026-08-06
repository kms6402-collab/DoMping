using System;
using System.Globalization;
using System.Windows.Data;

namespace DoMping.Classes;

public class ProbeStatusToGlyphConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		switch ((ProbeStatus)value)
		{
		case ProbeStatus.Up:
			return "t";
		case ProbeStatus.Down:
			return "u";
		case ProbeStatus.LatencyHigh:
		case ProbeStatus.Indeterminate:
			return "i";
		default:
			return string.Empty;
		}
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
