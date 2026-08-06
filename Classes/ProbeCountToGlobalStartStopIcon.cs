using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace DoMping.Classes;

public class ProbeCountToGlobalStartStopIcon : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if ((int)value > 0)
		{
			return (DrawingImage)Application.Current.Resources["icon.stop-circle"];
		}
		return (DrawingImage)Application.Current.Resources["icon.play"];
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
