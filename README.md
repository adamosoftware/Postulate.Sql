# Postulate.Sql

Nuget package: **Postulate.Sql**

I created this package as a refactoring of parts of [PostulateORM](https://github.com/adamosoftware/PostulateORM) to make query and SQL-related features available on their own. This package has three main attractions:

- [DynamicWhere](https://github.com/adamosoftware/Postulate.Sql/blob/master/Postulate.Sql/DynamicWhere.cs) is used for building and executing queries with WHERE clauses determined by the parameters passed to the query.

- [PagedQuery](https://github.com/adamosoftware/Postulate.Sql/blob/master/Postulate.Sql/PagedQuery.cs) is used for building queries that return a specific "page" of a fixed number of records. The static method `PagedQuery.Build` takes any query and encloses it in syntax that makes it "pageable."

- [Query&lt;TResult&gt;](https://github.com/adamosoftware/Postulate.Sql/blob/master/Postulate.Sql/Abstract/Query.cs) abstract class is used to return strongly-typed results from an inline SQL query. There's support for dynamic WHERE and ORDER BY clauses without SQL injection exposure.

## Using DynamicWhere

The `DynamicWhere` static class has a `DynamicQuery<T>` extension method patterned after [Dapper's](https://github.com/StackExchange/Dapper) Query&lt;T&gt; method. It lets you execute queries that look like this -- where curly braces enclose the WHERE clause and terms within the clause. The query executes according to the parameters that are passed to it.

    SELECT 
        SCHEMA_NAME([schema_id]) AS [Schema], [name] AS [Name], [object_id] AS [ObjectId]
    FROM 
        [sys].[tables] [t]	
    where {
        { SCHEMA_NAME([schema_id])=@schema }
        { [name] LIKE '%'+@table+'%' }
        { EXISTS(SELECT 1 FROM [sys].[columns] WHERE [object_id]=[t].[object_id] AND [name] LIKE '%'+@column+'%') }
    }			    		
    ORDER BY [name]
    
## Using PagedQuery

Use the `PagedQuery.Build` method to turn an ordinary select query into one that is "pageable." For example:

    var result = PagedQuery.Build("SELECT * FROM sys.tables", "[name]", 10, 1);
    
returns

    WITH [source] AS (SELECT ROW_NUMBER() OVER(ORDER BY [name]) AS [RowNumber], * FROM sys.tables) SELECT * FROM [source] WHERE [RowNumber] BETWEEN 11 AND 20;
    
## Using Query&lt;TResult&gt;

Provides a way to use inline SQL while keeping it isolated from your application. More info to come soon....
