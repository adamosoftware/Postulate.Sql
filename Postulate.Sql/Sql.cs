using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Postulate.Sql
{
    public static class Sql
    {
        internal const string WhereReplaceToken = "{*}";

        public static IEnumerable<T> DynamicQuery<T>(
            this IDbConnection connection, string query, object parameters, 
            IDbTransaction transaction = null, bool buffered = true, int? commandTimeout = null, CommandType? commandType = null)
        {
            return connection.Query<T>(BuildDynamicQuery(query, parameters), parameters, transaction, buffered, commandTimeout, commandType);
        }

        public static async Task<IEnumerable<T>> DynamicQueryAsync<T>(this IDbConnection connection, string query, object parameters,
            IDbTransaction transaction = null, bool buffered = true, int? commandTimeout = null, CommandType? commandType = null)
        {
            return await connection.QueryAsync<T>(BuildDynamicQuery(query, parameters), parameters, transaction, commandTimeout, commandType);
        }

        public static string BuildDynamicQuery(string sql, object parameters)
        {
            string queryTemplate, prepend;            
            var terms = ParseWhereBlock(sql, parameters, out queryTemplate, out prepend);
            if (terms.Any())
            {
                IEnumerable<WhereClauseTerm> includedTerms;
                return queryTemplate.Replace(WhereReplaceToken, WhereClauseBase(prepend, terms, out includedTerms));
            }
            else
            {
                parameters = null;
                return sql;
            }
        }

        private static IEnumerable<WhereClauseTerm> ParseWhereBlock(string sql, object parameters, out string queryTemplate, out string prepend)
        {            
            Dictionary<string, string> prependMap = new Dictionary<string, string>()
            {
                { @"where", "WHERE " },
                { @"andWhere", "AND " }
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

                        Type paramType = parameters.GetType();
                        Dictionary<string, PropertyInfo> props = paramType.GetProperties().ToDictionary(pi => pi.Name);
                        foreach (Match match in termMatches)
                        {
                            string paramName = WhereClauseTerm.GetParameterName(match.Value).Substring(1);
                            if (props.ContainsKey(paramName))
                            {
                                terms.Add(new WhereClauseTerm(
                                    props[paramName].GetValue(parameters), 
                                    TrimBraces(match.Value)));
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

        private static string TrimBraces(string value)
        {
            string result = value;
            if (value.StartsWith("{")) result = result.Substring(1);
            if (value.EndsWith("}")) result = result.Substring(0, result.Length - 1);
            return result.Trim();
        }

        public static string AndWhereClause(IEnumerable<WhereClauseTerm> terms, out DynamicParameters parameters)
        {
            return WhereClauseInner("AND ", terms, out parameters);
        }

        public static string WhereClause(IEnumerable<WhereClauseTerm> terms, out DynamicParameters parameters)
        {
            return WhereClauseInner("WHERE ", terms, out parameters);
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
