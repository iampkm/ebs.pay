using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CQSS.Pay.Util.Extension
{
    public static class StringExtension
    {
        /// <summary>
        /// 字符串是否全是数字
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool IsNumber(this string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            string reg = @"^\d+$";
            return Regex.IsMatch(value, reg);
        }

        /// <summary>
        /// 字符串是否是实数
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool IsDigit(this string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            string reg = @"^[-+]?\d+(\.\d+)?$";
            return Regex.IsMatch(value, reg);
        }

        /// <summary>
        /// 字符串是否是整数
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool IsInteger(this string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            string reg = @"^[-+]?(0|[1-9][0-9]*)$";
            return Regex.IsMatch(value, reg);
        }

        /// <summary>
        /// 字符串是否是网址
        /// </summary>
        public static bool IsUrl(this string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            string reg = @"^(http|ftp|https):\/\/[\w\-_]+(\.[\w\-_]+)+([\w\-\.,@?^=%&amp;:/~\+#]*[\w\-\@?^=%&amp;/~\+#])?$";
            return Regex.IsMatch(value, reg);
        }
    }
}
