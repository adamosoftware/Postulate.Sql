using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Postulate.Extensions
{
    internal static class StringExtensions
    {
        internal const string OrderByToken = "{orderBy}";
        internal const string WhereToken = "{where}";
        internal const string AndWhereToken = "{andWhere}";

        internal static bool ContainsAny(this string input, IEnumerable<string> substrings, out string substring)
        {
            substring = null;

            foreach (string item in substrings)
            {
                if (input.Contains(item))
                {
                    substring = item;
                    return true;
                }
            }

            return false;
        }

        internal static string ClearTokens(this string sql)
        {
            string result = sql;
            foreach (var token in new string[] { WhereToken, AndWhereToken, OrderByToken }) result = result.Replace(token, string.Empty);
            return result;
        }
    }
}
