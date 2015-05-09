using System.ComponentModel;

namespace Toxy.Common
{
	public enum SpellcheckLanguage
	{
		/*
		 * The languages below are the only 4 supported by the WPF spellcheck feature.
		 * See here: http://stackoverflow.com/a/209266/3071361
		 */

		[Description("en-US")]
		English,
		[Description("fr")]
		French,
		[Description("de")]
		German,
		[Description("es")]
		Spanish
	}
}
