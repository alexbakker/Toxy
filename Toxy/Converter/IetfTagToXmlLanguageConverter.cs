using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace Toxy.Converter
{
	public class IetfTagToXmlLanguageConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var ietfTag = value as string;

			return ietfTag == null ? Binding.DoNothing : XmlLanguage.GetLanguage(ietfTag);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var xmlLang = value as XmlLanguage;

			return xmlLang == null ? Binding.DoNothing : xmlLang.IetfLanguageTag;
		}
	}
}
