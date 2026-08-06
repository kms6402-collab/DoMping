using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace DoMping.Classes;

public class StringToBrushConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (string.IsNullOrWhiteSpace((string)value))
		{
			return Binding.DoNothing;
		}
		try
		{
			return (Brush)new BrushConverter().ConvertFromString((string)value);
		}
		catch
		{
			return Binding.DoNothing;
		}
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
