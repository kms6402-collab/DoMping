using System;
using System.Globalization;
using System.Windows.Data;

namespace DoMping.Classes;

public class HostnameFontsizeConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if ((double)value > 250.0)
		{
			return 18;
		}
		if ((double)value > 225.0)
		{
			return 17;
		}
		if ((double)value > 200.0)
		{
			return 16;
		}
		if ((double)value > 175.0)
		{
			return 15;
		}
		if ((double)value > 150.0)
		{
			return 14;
		}
		if ((double)value > 125.0)
		{
			return 13;
		}
		return 12.5;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
