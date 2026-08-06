using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace DoMping.Classes;

public class LatencyToBarBrushConverter : IValueConverter
{
	private static readonly SolidColorBrush DownBrush = (SolidColorBrush)new BrushConverter().ConvertFromString("#ff5d5d");

	private static readonly SolidColorBrush SlowBrush = (SolidColorBrush)new BrushConverter().ConvertFromString("#ffcf5c");

	private static readonly SolidColorBrush UpBrush = (SolidColorBrush)new BrushConverter().ConvertFromString("#33ff99");

	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		double ms = 0.0;
		try
		{
			ms = System.Convert.ToDouble(value, culture);
		}
		catch
		{
			ms = 0.0;
		}
		if (ms <= 0.0)
		{
			return DownBrush;
		}
		if (ms >= 150.0)
		{
			return SlowBrush;
		}
		return UpBrush;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
