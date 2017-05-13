using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Postulate.Sql
{
    public static class DynamicWhere
    {
        internal const string WhereReplaceToken = "{*}";

        public static IEnumerable<T> DynamicQuery<T>(
            this IDbConnection connection, string query, Dictionary<string, object> parameters,
            IDbTransaction transaction = null, bool buffered = true, int? commandTimeout = null, CommandType? commandType = null)
        {
            return connection.Query<T>(BuildQuery(query, parameters), parameters.ToObject(), transaction, buffered, commandTimeout, commandType);
        }

        public static IEnumerable<T> DynamicQuery<T>(
            this IDbConnection connection, string query, object parameters, 
            IDbTransaction transaction = null, bool buffered = true, int? commandTimeout = null, CommandType? commandType = null)
        {
            return connection.Query<T>(BuildQuery(query, parameters), parameters, transaction, buffered, commandTimeout, commandType);
        }

        public static async Task<IEnumerable<T>> DynamicQueryAsync<T>(
            this IDbConnection connection, string query, object parameters,
            IDbTransaction transaction = null, bool buffered = true, int? commandTimeout = null, CommandType? commandType = null)
        {
            return await connection.QueryAsync<T>(BuildQuery(query, parameters), parameters, transaction, commandTimeout, commandType);
        }

        public static string BuildQuery(string sql, object parameters)
        {
            var dictionary = ToDictionary(parameters);
            return BuildQuery(sql, dictionary);
        }

        public static string BuildQuery(string sql, Dictionary<string, object> parameters)
        {
            string queryTemplate, prepend;
            var terms = ParseWhereBlock(sql, parameters, out queryTemplate, out prepend);
            
            IEnumerable<WhereClauseTerm> includedTerms;
            return queryTemplate.Replace(WhereReplaceToken, WhereClauseBase(prepend, terms, out includedTerms));            
        }

        public static string AndWhereClause(IEnumerable<WhereClauseTerm> terms, out DynamicParameters parameters)
        {
            return WhereClauseInner("AND ", terms, out parameters);
        }

        public static string WhereClause(IEnumerable<WhereClauseTerm> terms, out DynamicParameters parameters)
        {
            return WhereClauseInner("WHERE ", terms, out parameters);
        }

        private static IEnumerable<WhereClauseTerm> ParseWhereBlock(string sql, Dictionary<string, object> parameters, out string queryTemplate, out string prepend)
        {
            Dictionary<string, string> prependMap = new Dictionary<string, string>()
            {
                { "where", "WHERE " },
                { "andWhere", "AND " }
            };

            foreach (string blockStart in prependMap.Keys)
            {
                var startMatch = Regex.Match(sql, blockStart + @"\s*{\s*{");
                if (startMatch != null)
                {
                    prepend = prependMap[blockStart];
                    int leftIndex = startMatch.Index;
                    var endMatch = Regex.Match(sql.Substring(startMatch.Index + startMatch.Length), @"}\s*}");
                    if (endMatch != null)
                    {
                        int rightIndex = leftIndex + startMatch.Length + endMatch.Index + endMatch.Length;
                        List<WhereClauseTerm> terms = new List<WhereClauseTerm>();

                        string termBlock = "{ " + sql.Substring(startMatch.Index + startMatch.Length, endMatch.Index) + " }";
                        var termMatches = Regex.Matches(termBlock, "(?<!{)({[^{\r\n]*})(?!{)");

                        foreach (Match match in termMatches)
                        {
                            string paramName = WhereClauseTerm.GetParameterName(match.Value).Substring(1);
                            if (parameters.ContainsKey(paramName))
                            {
                                terms.Add(new WhereClauseTerm(parameters[paramName], TrimBraces(match.Value)));
                            }
                        }

                        queryTemplate = sql.Substring(0, leftIndex) + WhereReplaceToken + sql.Substring(rightIndex);
                        return terms;
                    }

                }
            }

            // if you make it here, it means there was no dynamic WHERE block, so we just run the original query
            queryTemplate = sql;
            prepend = null;
            return null;
        }

        private static IEnumerable<WhereClauseTerm> ParseWhereBlock(string sql, object parameters, out string queryTemplate, out string prepend)
        {
            Dictionary<string, object> props = ToDictionary(parameters);
            return ParseWhereBlock(sql, props, out queryTemplate, out prepend);
        }

        public static object ToObject(this Dictionary<string, object> dictionary)
        {
            // thanks to http://stackoverflow.com/questions/7595416/convert-dictionarystring-object-to-anonymous-object

            var expando = new ExpandoObject();
            var properties = (ICollection<KeyValuePair<string, object>>)expando;
            foreach (var keyPair in dictionary) properties.Add(keyPair);
            return expando;
        }

        public static Dictionary<string, object> ToDictionary(this object @object)
        {
            Type paramType = @object.GetType();
            Dictionary<string, object> props = paramType.GetProperties()
                .Where(pi => !string.IsNullOrEmpty(pi.GetValue(@object).ToString()))
                .ToDictionary(pi => pi.Name, pi => pi.GetValue(@object));
            return props;
        }

        public static Dictionary<string, object> ToDictionary(this IEnumerable<IDataParameter> parameters)
        {
            return parameters
                .Where(p => p.Value != null && p.Value != DBNull.Value)
                .ToDictionary(p => p.ParameterName, p => p.Value);
        }

        private static string TrimBraces(string value)
        {
            string result = value;
            if (value.StartsWith("{")) result = result.Substring(1);
            if (value.EndsWith("}")) result = result.Substring(0, result.Length - 1);
            return result.Trim();
        }

        private static string WhereClauseBase(string prepend, IEnumerable<WhereClauseTerm> terms, out IEnumerable<WhereClauseTerm> included)
        {
            included = terms.Where(t => t.Value != null);
            if (!included.Any()) return null;
            return prepend + string.Join(" AND ", included.Select(t => t.Expression));
        }

        private static string WhereClauseInner(string prepend, IEnumerable<WhereClauseTerm> terms, out DynamicParameters parameters)
        {
            IEnumerable<WhereClauseTerm> included;
            string result = WhereClauseBase(prepend, terms, out included);

            parameters = new DynamicParameters();
            foreach (WhereClauseTerm term in included) parameters.Add(term.GetParameterName(), term.Value);

            return result;
        }
    }

    public class WhereClauseTerm
    {        
        public string Expression { get; set; }
        public object Value { get; set; }

        public WhereClauseTerm(object value, string expression)
        {     
            Expression = expression;
            Value = value;
        }

        public string GetParameterName()
        {
            return GetParameterName(Expression);
        }

        public static string GetParameterName(string expression)
        {
            // thanks to http://stackoverflow.com/questions/307929/regex-for-parsing-sql-parameters
            var matches = Regex.Matches(expression, "@([a-zA-Z][a-zA-Z0-9_]*)");
            return matches.OfType<Match>().Select(m => m.Value).ToArray().First();
        }
    }
}
