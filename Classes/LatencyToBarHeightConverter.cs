using System;
using System.Globalization;
using System.Windows.Data;

namespace DoMping.Classes;

public class LatencyToBarHeightConverter : IValueConverter
{
	private const double MinHeight = 4.0;

	private const double MaxHeight = 52.0;

	private const double ScaleMs = 200.0;

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
		double fraction = Math.Min(Math.Max(ms, 0.0), ScaleMs) / ScaleMs;
		return MinHeight + fraction * (MaxHeight - MinHeight);
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
