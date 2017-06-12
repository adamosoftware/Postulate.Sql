using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Postulate.Sql.Extensions
{
    public static class StringExtensions
    {
        public static IEnumerable<string> GetParameterNames(this string sql, bool cleaned = false)
        {
            var matches = Regex.Matches(sql, InternalStringExtensions.SqlParamRegex);
            return matches.OfType<Match>().Select(m => (cleaned) ? m.Value.Substring(1) : m.Value);
        }

        public static string EnumToCase<TEnum>(this string expression)
        {
            string result = $"CASE {expression}";

            var names = Enum.GetNames(typeof(TEnum));

            int index = 0;
            foreach (var value in Enum.GetValues(typeof(TEnum)))
            {
                result += $" WHEN {Convert.ToInt32(value)} THEN '{names[index]}'\r\n";
                index++;
            }

            result += "END";

            return result;
        }
    }
}
