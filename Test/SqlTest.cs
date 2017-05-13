using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data;
using Dapper;
using System.Linq;
using System.Collections.Generic;
using Postulate.Sql;
using System.Data.SqlClient;

namespace Testing
{
    [TestClass]
    public class SqlTest
    {
        private SqlConnection GetConnection()
        {
            return new SqlConnection("Data Source=localhost;Database=PostulateTest;Integrated Security=SSPI");
        }

        [TestMethod]
        public void DynamicWhereClausesTable()
        {
            using (IDbConnection cn = GetConnection())
            {
                cn.Open();
                var results = InnerQuery(cn, "Org", null);
                Assert.IsTrue(results.Any());
            }
        }

        [TestMethod]
        public void DynamicWhereClausesColumn()
        {
            using (IDbConnection cn = GetConnection())
            {
                cn.Open();
                var results = InnerQuery(cn, null, "last");
                Assert.IsTrue(results.Any());
            }
        }

        private static IEnumerable<TableInfo> InnerQuery(IDbConnection cn, string tableName, string columnName)
        {
            DynamicParameters p;            
            var results = cn.Query<TableInfo>(
                $@"SELECT 
					    SCHEMA_NAME([schema_id]) AS [Schema], [name] AS [Name], [object_id] AS [ObjectId]
				    FROM 
					    [sys].[tables] [t]				    		
				    {DynamicWhere.Clause(new DynamicWhere.Term[]
                    {
                        new DynamicWhere.Term(tableName, "[name] LIKE '%'+@table+'%'"),
                        new DynamicWhere.Term(columnName, "EXISTS(SELECT 1 FROM [sys].[columns] WHERE [object_id]=[t].[object_id] AND [name] LIKE '%'+@column+'%')")
                    }, out p)}					
				ORDER BY 
					[name]", p);
            return results;
        }

        private static IEnumerable<TableInfo> InnerDynamicWhere(IDbConnection cn, object parameters)
        {
            var results = cn.Query<TableInfo>(GetQuery(parameters));
            return results;
        }

        private static string GetQuery(object parameters)
        {
            return DynamicWhere.BuildQuery(
                @"SELECT 
					SCHEMA_NAME([schema_id]) AS [Schema], [name] AS [Name], [object_id] AS [ObjectId]
				FROM 
					[sys].[tables] [t]				    		
                where {
                    { [name] LIKE '%'+@table+'%' }
                    { EXISTS(SELECT 1 FROM [sys].[columns] WHERE [object_id]=[t].[object_id] AND [name] LIKE '%'+@column+'%') }
                }", parameters);
        }

        [TestMethod]
        public void DynamicQueryForTable()
        {            
            using (IDbConnection cn = GetConnection())
            {
                cn.Open();
                var param = new { table = "Organization" };
                var results = cn.Query<TableInfo>(GetQuery(param), param);
                Assert.IsTrue(results.Any());
            }
        }
    }

    internal class TableInfo
    {
        public string Schema { get; set; }
        public string Name { get; set; }
        public int ObjectId { get; set; }
    }

}
