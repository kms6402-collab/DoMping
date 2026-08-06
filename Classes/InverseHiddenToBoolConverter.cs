using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DoMping.Classes;

public class InverseHiddenToBoolConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return (Visibility)value == Visibility.Hidden || (Visibility)value == Visibility.Collapsed;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotSupportedException();
	}
}
