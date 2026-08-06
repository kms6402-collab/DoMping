using System;
using System.Globalization;
using System.Windows.Data;

namespace DoMping.Classes;

public class AliasOrHostnameConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		string alias = (values.Length > 0) ? values[0] as string : null;
		string hostname = (values.Length > 1) ? values[1] as string : null;
		if (!string.IsNullOrEmpty(alias))
		{
			return alias;
		}
		return hostname ?? string.Empty;
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
