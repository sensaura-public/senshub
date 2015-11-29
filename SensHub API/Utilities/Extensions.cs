using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SensHub.Plugins.Utilities
{
	public static class Extensions
	{
		private static readonly Regex IDENT_REGEX = new Regex(@"^[a-zA-Z0-9\-_]+$");

		/// <summary>
		/// Determine if the string is a valid identifier.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static bool IsValidIdentifier(this string value)
		{
			return IDENT_REGEX.IsMatch(value);
		}
	}
}
