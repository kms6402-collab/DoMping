using System;
using System.Globalization;
using System.Windows.Data;

namespace DoMping.Classes;

public class ProbeTypeToFontSizeConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if ((ProbeType)value == ProbeType.Ping || (ProbeType)value == ProbeType.Tcp || (ProbeType)value == ProbeType.Service)
		{
			return ApplicationOptions.FontSize_Probe;
		}
		return ApplicationOptions.FontSize_Scanner;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
