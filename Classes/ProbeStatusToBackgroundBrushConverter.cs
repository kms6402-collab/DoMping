using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace DoMping.Classes;

public class ProbeStatusToBackgroundBrushConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		switch ((ProbeStatus)value)
		{
		case ProbeStatus.Up:
			return (Brush)new BrushConverter().ConvertFromString(ApplicationOptions.BackgroundColor_Probe_Up);
		case ProbeStatus.Down:
			return (Brush)new BrushConverter().ConvertFromString(ApplicationOptions.BackgroundColor_Probe_Down);
		case ProbeStatus.Error:
			return (Brush)new BrushConverter().ConvertFromString(ApplicationOptions.BackgroundColor_Probe_Error);
		case ProbeStatus.LatencyHigh:
		case ProbeStatus.Indeterminate:
			return (Brush)new BrushConverter().ConvertFromString(ApplicationOptions.BackgroundColor_Probe_Indeterminate);
		case ProbeStatus.Scanner:
			return (Brush)new BrushConverter().ConvertFromString(ApplicationOptions.BackgroundColor_Probe_Scanner);
		default:
			return (Brush)new BrushConverter().ConvertFromString(ApplicationOptions.BackgroundColor_Probe_Inactive);
		}
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
