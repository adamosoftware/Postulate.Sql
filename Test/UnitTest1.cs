using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data;
using System.Data.SqlClient;
using Postulate.Sql;
using System.Linq;

namespace Test
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void GetDbObjects()
        {
            using (SqlConnection cn = new SqlConnection("Data Source=localhost;Database=PostulateTest;Integrated Security=SSPI"))
            {
                cn.Open();
                var objects = cn.DynamicQuery<DbObject>(
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
				        [name]", new { schema = "", tableName = "Org", columnName = "" });

                Assert.IsTrue(objects.Any());
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
            return $"{Schema}.{Name}";
        }
    }
}
