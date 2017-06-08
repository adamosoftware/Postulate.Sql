using Microsoft.VisualStudio.TestTools.UnitTesting;
using Postulate;
using Postulate.Sql.Attributes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Testing.Models;
using Postulate.Sql.Abstract;
using Postulate.Sql.Extensions;

namespace Testing
{
    [TestClass]
    public class Queries
    {
        [TestMethod]
        public void AnyOrgs()
        {
            var allOrgs = new AllOrgs().Execute();
            Assert.IsTrue(allOrgs.Any());
        }

        [TestMethod]
        public void OrgsWithName()
        {
            var orgs = new OrgsWithName("sample").Execute();
            Assert.IsTrue(orgs.Any());
        }

        [TestMethod]
        public void OrgsWithName2()
        {
            var orgs = new OrgsWithName2() { Name = "sample" }.Execute();
            Assert.IsTrue(orgs.Any());
        }

        [TestMethod]
        public void OrgsPaged()
        {
            int totalRecordCount = new PostulateQuery<int>("SELECT COUNT(1) FROM [dbo].[Organization]").ExecuteSingle();
            int testRecords = 0;

            IEnumerable<Organization> orgs = null;
            int page = 0;
            do
            {
                orgs = new AllOrgs().Execute("[Name]", 3, page);
                testRecords += orgs.Count();
                page++;
            } while (orgs.Any());

            Assert.IsTrue(testRecords == totalRecordCount);
        }

        [TestMethod]
        public void OrgsWhere()
        {
            Organization org = new Organization() { Name = "Sample Org For You", BillingRate = 1 };

            var db = new PostulateDb();
            db.Save(org);

            var orgs = new OrgsWhere() { Name = "sample" }.Execute();
            Assert.IsTrue(orgs.Any());

            db.Delete<Organization>(org.Id);
        }

        [TestMethod]
        public void QueryTestMethod()
        {
            PostulateQuery<Organization>[] queries = new PostulateQuery<Organization>[]
            {
                new AllOrgs(), new OrgsWithName2(), new OrgsWhere()
            };

            using (IDbConnection cn = new PostulateDb().GetConnection())
            {
                cn.Open();
                foreach (var query in queries) query.Test(cn);
            }
        }

        [TestMethod]
        public void TestAndWhereQuery()
        {
            var results = new AllTables().Execute();
            Assert.IsTrue(results.Any());
        }

        [TestMethod]
        public void TestGetParameterNamesDefault()
        {
            var query = "SELECT [Id] FROM [Somewhere] WHERE [Something]=@this AND [OtherThing]=@that";
            var paramNames = query.GetParameterNames();
            Assert.IsTrue(paramNames.SequenceEqual(new string[] { "@this", "@that" }));
        }

        [TestMethod]
        public void TestGetParameterNamesCleaned()
        {
            var query = "SELECT [Id] FROM [Somewhere] WHERE [Something]=@this AND [OtherThing]=@that";
            var paramNames = query.GetParameterNames(true);
            Assert.IsTrue(paramNames.SequenceEqual(new string[] { "this", "that" }));
        }

        [TestMethod]
        public void QueryWithBuiltInParams()
        {
            var items = new ItemsWhereOrg();
            items.OrgId = 1;
            var results = items.Execute();
            Assert.IsTrue(items.ResolvedSql.Equals("SELECT * FROM [Item] WHERE [OrganizationId]=@orgId  ORDER BY [Name]"));
        }

        [TestMethod]
        public void QueryWithBuiltInAndAddedParams()
        {
            var items = new ItemsWhereOrg();
            items.OrgId = 1;
            items.Name = "whatever";
            var results = items.Execute();
            Assert.IsTrue(items.ResolvedSql.Equals("SELECT * FROM [Item] WHERE [OrganizationId]=@orgId AND [Name] LIKE '%'+@name+'%' ORDER BY [Name]"));
        }
    }

    public class PostulateQuery<TResult> : Query<TResult>
    {
        public PostulateQuery(string sql) : base(sql, () => { return new PostulateDb().GetConnection(); })
        {
        }

        //public override string[] SortOptions { get { return new string[] { "[Name]" }; }

        public override SortOption[] SortOptions => new SortOption[]
        {
            new SortOption() { Text = "Last Name", Expression = "[LastName] ASC" }
        };
    }

    public class AllTables : PostulateQuery<SysTable>
    {
        public AllTables() : base("SELECT * FROM sys.tables WHERE SCHEMA_NAME([schema_id])=@schema {andWhere} ORDER BY [name]")
        {
        }

        public string Schema { get; set; } = "dbo";
    }

    public class AllOrgs : PostulateQuery<Organization>
    {
        public AllOrgs() : base("SELECT * FROM [dbo].[Organization] {where}")
        {
        }

        [Where("[Name] LIKE '%' + @name + '%'")]
        public string Name { get; set; }
    }

    public class OrgsWithName : PostulateQuery<Organization>
    {
        public OrgsWithName(string name) : base("SELECT * FROM [dbo].[Organization] WHERE [Name] LIKE '%' + @name + '%'")
        {
            Name = name;
        }

        public string Name { get; set; }        
    }

    public class OrgsWithName2 : PostulateQuery<Organization>
    {
        public OrgsWithName2() : base("SELECT * FROM [dbo].[Organization] WHERE [Name] LIKE '%' + @name + '%'")
        {
        }

        public string Name { get; set; }
    }

    public class OrgsWhere : PostulateQuery<Organization>
    {
        public OrgsWhere() : base("SELECT * FROM [dbo].[Organization] {where}")
        {
        }

        [Where("[Name] LIKE '%' + @name + '%'")]
        public string Name { get; set; }
    }

    public class ItemsWhereOrg : PostulateQuery<Item>
    {
        public ItemsWhereOrg() : base("SELECT * FROM [Item] WHERE [OrganizationId]=@orgId {andWhere} ORDER BY [Name]")
        {
        }

        public int OrgId { get; set; }

        [Where("[Name] LIKE '%'+@name+'%'")]
        public string Name { get; set; }
    }

    public class SysTable
    {
        public string name { get; set; }
        public int object_id { get; set; }
        public int? principal_id { get; set; }
        public int schema_id { get; set; }
        public int parent_object_id { get; set; }
        public string type { get; set; }
        public string type_desc { get; set; }
        public DateTime create_date { get; set; }
        public DateTime modify_date { get; set; }
        public bool is_ms_shipped { get; set; }
        public bool is_published { get; set; }
        public bool is_schema_published { get; set; }
        public int lob_data_space_id { get; set; }
        public int? filestream_data_space_id { get; set; }
        public int max_column_id_used { get; set; }
        public bool lock_on_bulk_load { get; set; }
        public bool? uses_ansi_nulls { get; set; }
        public bool? is_replicated { get; set; }
        public bool? has_replication_filter { get; set; }
        public bool? is_merge_published { get; set; }
        public bool? is_sync_tran_subscribed { get; set; }
        public bool has_unchecked_assembly_data { get; set; }
        public int? text_in_row_limit { get; set; }
        public bool? large_value_types_out_of_row { get; set; }
        public bool? is_tracked_by_cdc { get; set; }
        public byte? lock_escalation { get; set; }
        public string lock_escalation_desc { get; set; }
        public bool? is_filetable { get; set; }
        public bool? is_memory_optimized { get; set; }
        public byte? durability { get; set; }
        public string durability_desc { get; set; }
        public byte? temporal_type { get; set; }
        public string temporal_type_desc { get; set; }
        public int? history_table_id { get; set; }
        public bool? is_remote_data_archive_enabled { get; set; }
        public bool is_external { get; set; }
        public int? history_retention_period { get; set; }
        public int? history_retention_period_unit { get; set; }
        public string history_retention_period_unit_desc { get; set; }
        public bool? is_node { get; set; }
        public bool? is_edge { get; set; }
    }

}
