using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using System.Reflection;

namespace Postulate.Sql.Extensions
{
    public static class SqlConnectionExtensions
    {
        public static DataTable QueryDataTable(this SqlConnection connection, string query, object parameters)
        {
            return QueryDataTable(connection, query, parameters.ToParameters());
        }

        public static DataTable QueryDataTable(this SqlConnection connection, string query, params SqlParameter[] parameters)
        {
            DataTable results = new DataTable();
            using (SqlCommand cmd = new SqlCommand(query, connection))
            {
                cmd.Parameters.AddRange(parameters);
                using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                {
                    adapter.Fill(results);
                }
            }
            return results;
        }

        public static SqlParameter[] ToParameters(this object @object)
        {
            if (@object == null) return null;
            PropertyInfo[] props = @object.GetType().GetProperties();
            return props.Select(p => 
            {
                object value = p.GetValue(@object);
                if (value == null) value = DBNull.Value;
                return new SqlParameter(p.Name, value);
            }).ToArray();
        }
    }
}
