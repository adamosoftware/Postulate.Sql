using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Postulate.Sql
{
    public static class PagedQuery
    {
        public static string Build(string query, string orderBy, int pageSize, int pageNumber)
        {
            int startRecord = (pageNumber * pageSize) + 1;
            int endRecord = (pageNumber * pageSize) + pageSize;
            return $"WITH [source] AS ({InsertRowNumberColumn(query, orderBy)}) SELECT * FROM [source] WHERE [RowNumber] BETWEEN {startRecord} AND {endRecord};";
        }

        private static string InsertRowNumberColumn(string query, string orderBy)
        {
            StringBuilder sb = new StringBuilder(query);
            int insertPoint = query.ToLower().IndexOf("select ") + "select ".Length;
            sb.Insert(insertPoint, $"ROW_NUMBER() OVER(ORDER BY {orderBy}) AS [RowNumber], ");
            return sb.ToString();
        }
    }
}
