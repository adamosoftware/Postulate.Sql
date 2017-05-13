# Postulate.Sql

I created this package as a refactoring of parts of [PostulateORM](https://github.com/adamosoftware/PostulateORM) to make query and SQL-related features available on their own. This package has three main attractions:

- [DynamicWhere](https://github.com/adamosoftware/Postulate.Sql/blob/master/Postulate.Sql/DynamicWhere.cs) is used for building and executing queries that use WHERE clauses determined by the parameters passed to the query.

- [PagedQuery](https://github.com/adamosoftware/Postulate.Sql/blob/master/Postulate.Sql/PagedQuery.cs) is used for building queries that return a specific "page" of a fixed number of records.

- [Query&lt;TResult&gt;](https://github.com/adamosoftware/Postulate.Sql/blob/master/Postulate.Sql/Abstract/Query.cs) abstract class is used to return strongly-typed results from an inline SQL query.
