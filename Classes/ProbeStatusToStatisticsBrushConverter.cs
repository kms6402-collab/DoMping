using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace DoMping.Classes;

public class ProbeStatusToStatisticsBrushConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		switch ((ProbeStatus)value)
		{
		case ProbeStatus.Up:
			return (Brush)new BrushConverter().ConvertFromString(ApplicationOptions.ForegroundColor_Stats_Up);
		case ProbeStatus.Down:
			return (Brush)new BrushConverter().ConvertFromString(ApplicationOptions.ForegroundColor_Stats_Down);
		case ProbeStatus.Error:
			return (Brush)new BrushConverter().ConvertFromString(ApplicationOptions.ForegroundColor_Stats_Error);
		case ProbeStatus.LatencyHigh:
		case ProbeStatus.Indeterminate:
			return (Brush)new BrushConverter().ConvertFromString(ApplicationOptions.ForegroundColor_Stats_Indeterminate);
		default:
			return (Brush)new BrushConverter().ConvertFromString(ApplicationOptions.ForegroundColor_Stats_Inactive);
		}
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
