using Postulate.Sql.Extensions;
using System.Text;
using System.Text.RegularExpressions;

namespace Postulate.Sql
{
    public static class PagedQuery
    {
        public static string Build(string query, string orderBy, int pageSize, int pageNumber)
        {
            int startRecord = (pageNumber * pageSize) + 1;
            int endRecord = (pageNumber * pageSize) + pageSize;
            string result = $"WITH [source] AS ({InsertRowNumberColumn(query, orderBy)}) SELECT * FROM [source] WHERE [RowNumber] BETWEEN {startRecord} AND {endRecord};";
            return result.ClearTokens();
        }

        private static string InsertRowNumberColumn(string query, string orderBy)
        {
            StringBuilder sb = new StringBuilder(query);

            var matches = Regex.Matches(query, @"select\s", RegexOptions.IgnoreCase);
            if (matches.Count > 0)
            {
                var first = matches[0];
                int insertPoint = first.Length + 1;
                sb.Insert(insertPoint, $"ROW_NUMBER() OVER(ORDER BY {orderBy}) AS [RowNumber], ");
            }

            return sb.ToString();
        }
    }
}
