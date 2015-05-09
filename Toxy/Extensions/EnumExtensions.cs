using System;
using System.ComponentModel;

namespace Toxy.Extensions
{
	public static class EnumExtensions
	{
		/// <summary>
		/// Takes the specified enum and returns the content of its description attribute, or the name of the enum
		/// if no description has been set
		/// </summary>
		/// <param name="value">The enum of that the description should be returned</param>
		/// <returns>A string, containing the description of the specified enum, or its name converted to string</returns>
		public static string ToDescription(this Enum value)
		{
			var descAttribute = (DescriptionAttribute[]) (value.GetType().GetField(value.ToString()))
				.GetCustomAttributes(typeof (DescriptionAttribute), false);
			
			return (descAttribute.Length > 0) ? descAttribute[0].Description : value.ToString();
		}
	}
}
