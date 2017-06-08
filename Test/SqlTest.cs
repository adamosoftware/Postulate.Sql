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

        [TestMethod]
        public void TestPagedQuery()
        {
            string query = "SELECT * FROM sys.tables";
            string pagedQuery = PagedQuery.Build(query, "[name]", 10, 1);
            Assert.IsTrue(pagedQuery.Equals("WITH [source] AS (SELECT ROW_NUMBER() OVER(ORDER BY [name]) AS [RowNumber], * FROM sys.tables) SELECT * FROM [source] WHERE [RowNumber] BETWEEN 11 AND 20;"));
        }

        [TestMethod]
        public void TestFKRefDynamicWhereQuery()
        {
            string query = @"SELECT 
                [fk].[name] AS [ConstraintName], 
                SCHEMA_NAME([parent].[schema_id]) AS [ReferencedSchema],
                [parent].[name] AS [ReferencedTable],
                [refdcol].[name] AS [ReferencedColumn],
                SCHEMA_NAME([child].[schema_id]) AS [ReferencingSchema], 
                [child].[name] AS [ReferencingTable],
                [rfincol].[name] AS [ReferncingColumn]
            FROM 
                [sys].[foreign_keys] [fk] INNER JOIN [sys].[tables] [child] ON [fk].[parent_object_id]=[child].[object_id]
                INNER JOIN [sys].[tables] [parent] ON [fk].[referenced_object_id]=[parent].[object_id]
                INNER JOIN [sys].[foreign_key_columns] [fkcol] ON [fk].[parent_object_id]=[fkcol].[parent_object_id]        
                INNER JOIN [sys].[columns] [refdcol] ON 
                    [fkcol].[referenced_column_id]=[refdcol].[column_id] AND
                    [fkcol].[referenced_object_id]=[refdcol].[object_id]
                INNER JOIN [sys].[columns] [rfincol] ON 
                    [fkcol].[parent_column_id]=[rfincol].[column_id] AND
                    [fkcol].[parent_object_id]=[rfincol].[object_id]";

            using (SqlConnection cn = GetConnection())
            {
                cn.Open();
                var results = cn.DynamicQuery<ForeignKeyInfo>(query, null);
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

    internal class ForeignKeyInfo
    {
        public string ConstraintName { get; set; }
        public string ReferencedSchema { get; set; }
        public string ReferencedTable { get; set; }
        public string ReferencedColumn { get; set; }
        public string ReferencingSchema { get; set; }
        public string ReferencingTable { get; set; }
        public string ReferencingColumn { get; set; }
    }

}
