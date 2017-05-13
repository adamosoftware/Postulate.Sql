using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data;
using System.Data.SqlClient;
using Postulate.Sql;
using System.Linq;

namespace Test
{
    [TestClass]
    public class DbObjects
    {
        const string _connectionString = "Data Source=localhost; Database=PostulateTest;Integrated Security=SSPI";

        const string _dbObjectQuery =
            @"SELECT 
				SCHEMA_NAME([schema_id]) AS [Schema], [name] AS [Name], [object_id] AS [ObjectId]
			FROM 
				[sys].[tables] [t]	
            where {
                { SCHEMA_NAME([schema_id])=@schema }
                { [name] LIKE '%'+@table+'%' }
                { EXISTS(SELECT 1 FROM [sys].[columns] WHERE [object_id]=[t].[object_id] AND [name] LIKE '%'+@column+'%') }
            }			    		
			ORDER BY 
				[name]";

        [TestMethod]
        public void GetDbObjects()
        {
            using (SqlConnection cn = new SqlConnection(_connectionString))
            {
                cn.Open();
                var objects = cn.DynamicQuery<DbObject>(_dbObjectQuery, new { schema = "", tableName = "Org", columnName = "" });

                Assert.IsTrue(objects.Any());
            }
        }

        [TestMethod]
        public void TestQueryTableCriteria()
        {
            string query = DynamicWhere.BuildQuery(_dbObjectQuery, new { table = "org" });            
            Assert.IsTrue(query.Equals(
                @"SELECT 
				SCHEMA_NAME([schema_id]) AS [Schema], [name] AS [Name], [object_id] AS [ObjectId]
			FROM 
				[sys].[tables] [t]	
            WHERE [name] LIKE '%'+@table+'%'			    		
			ORDER BY 
				[name]"));
        }

        [TestMethod]
        public void DynamicQueryFromDictionary()
        {
            using (SqlConnection cn = new SqlConnection(_connectionString))
            {
                cn.Open();
                var results = cn.DynamicQuery<DbObject>(_dbObjectQuery, new { column = "ReorderQty" }.ToDictionary());
                Assert.IsTrue(results.Count() == 1);
            }
        }
    }

    public class DbObject
    {
        public string Schema { get; set; }
        public string Name { get; set; }
        public int ObjectId { get; set; }

        public DbObject()
        {
        }

        public DbObject(string schema, string name, int objectId = 0)
        {
            Schema = schema;
            Name = name;
            ObjectId = objectId;
        }

        public override string ToString()
        {
            return $"{ Schema}.{Name}";
        }
    }
}
