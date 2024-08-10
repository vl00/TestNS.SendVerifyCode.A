using System;
using System.Text;
using System.Text.RegularExpressions;

namespace Common
{
    public static partial class StringHelper
    {
        public static string TrimStr(this string str, string trimStr, bool trimEnd = true, bool repeatTrim = true, StringComparison comparisonType = StringComparison.OrdinalIgnoreCase)
        {
            int strLen;
            do
            {
                if (string.IsNullOrEmpty(str)) return str;
                strLen = str.Length;
                {
                    if (trimEnd)
                    {
                        var pos = str.LastIndexOf(trimStr, comparisonType);
                        if ((!(pos >= 0)) || (!(str.Length - trimStr.Length == pos))) break;
                        str = str.Substring(0, pos);
                    }
                    else
                    {
                        var pos = str.IndexOf(trimStr, comparisonType);
                        if (!(pos == 0)) break;
                        str = str.Substring(trimStr.Length, str.Length - trimStr.Length);
                    }
                }
            } while (repeatTrim && strLen > str.Length);
            return str;
        }

        public static string TrimEnd(this string str, string trimStr, bool repeatTrim = true, StringComparison comparisonType = StringComparison.OrdinalIgnoreCase)
        {
            return TrimStr(str, trimStr, true, repeatTrim, comparisonType);
        }

        public static string TrimStart(this string str, string trimStr, bool repeatTrim = true, StringComparison comparisonType = StringComparison.OrdinalIgnoreCase)
        {
            return TrimStr(str, trimStr, false, repeatTrim, comparisonType);
        }

        public static string Trim(this string str, string trimStr, bool repeatTrim = true, StringComparison comparisonType = StringComparison.OrdinalIgnoreCase)
        {
            return str.TrimStart(trimStr, repeatTrim, comparisonType).TrimEnd(trimStr, repeatTrim, comparisonType);
        }
    }
}