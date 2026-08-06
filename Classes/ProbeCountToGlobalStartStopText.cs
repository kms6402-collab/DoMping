using System;
using System.Globalization;
using System.Windows.Data;
using DoMping.Properties;

namespace DoMping.Classes;

public class ProbeCountToGlobalStartStopText : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if ((int)value > 0)
		{
			return Strings.Toolbar_StopAll;
		}
		return Strings.Toolbar_StartAll;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
