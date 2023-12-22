#nullable enable

using System.Text;
using System.Web;

namespace HttpRequestSpy.Assertions.HttpRequestAssertions;

internal abstract record QueryAssertion : IAssertHttpRequest
{
    private record QueryAsStringAssertion(string ExpectedQuery) : QueryAssertion
    {
        public override string ToExpectation() => $"Query {ExpectedQuery}";

        protected override AssertionResult Matches(string? requestUriQuery)
        {
            var expected = Parse(FormatQuery(ExpectedQuery));
            var actual = Parse(requestUriQuery);
            return CompareQueries(actual, expected);
        }

        private static string FormatQuery(string query) => query.StartsWith('?') ? query : $"?{query}";
    }
        
    private record QueryAsObjectAssertion(object ExpectedQuery) : QueryAssertion
    {
        public override string ToExpectation() => $"Query {ExpectedQuery}";

        protected override AssertionResult Matches(string? requestUriQuery)
        {
            var actualQuery = Parse(requestUriQuery);
            return CompareQueries(actualQuery, ExpectedQuery);
        }
    }
        
        
    private record SingleQueryParamAssertion(string ExpectedParamName, string? ExpectedParamValue) : QueryAssertion
    {
        public override string ToExpectation()
        {
            var valueExpectation = ExpectedParamValue is not null ? $" = {ExpectedParamValue}" : ""; 

            return $"Query Param {ExpectedParamName}{valueExpectation}";
        }
                

        protected override AssertionResult Matches(string? requestUriQuery)
        {
            if (Parse(requestUriQuery) is not Dictionary<string, string?> actualQuery)
            {
                return AssertionResult.Failure($"No query provided");
            }
                
            if (!actualQuery.ContainsKey(ExpectedParamName))
            {
                return AssertionResult.Failure($"A Query param {ExpectedParamName} was not found in actual query.");
            }

            if (ExpectedParamValue is null)
            {
                return AssertionResult.Success();
            }

            var actualParamValue = actualQuery[ExpectedParamName];

            if (ExpectedParamValue != actualParamValue)
            {
                return AssertionResult.Failure($"Value for Query Param {ExpectedParamName} does not match.", ExpectedParamValue, actualParamValue);
            }

            return AssertionResult.Success();
        }
    }
    public AssertionResult Matches(AssertableHttpRequestMessage request)
    {
        return Matches(request.InnerRequest.RequestUri?.Query);
    }

    public abstract string ToExpectation();

    protected abstract AssertionResult Matches(string? requestUriQuery);
        
    public static QueryAssertion FromString(string query) => new QueryAsStringAssertion(query);

    public static IAssertHttpRequest FromObject(object query) => new QueryAsObjectAssertion(query);
        
    public static IAssertHttpRequest SingleParam(string expectedQueryParamName, string? expectedQueryParamValue)
    {
        return new SingleQueryParamAssertion(expectedQueryParamName, expectedQueryParamValue);
    }
        
    private static object Parse(string? query)
    {
        if (query is null)
        {
            return new Dictionary<string, string?>();
        }

        var queryParams = HttpUtility.ParseQueryString(query);

        return queryParams.AllKeys.Where(k => k is not null)
            .ToDictionary(key => key!, key => queryParams[key]);
    }

    private static AssertionResult CompareQueries(object actualQuery, object expectedQuery)
    {
        var differences = actualQuery.CompareObjectAsJsonTo(expectedQuery);

        if (differences.IsEmpty)
        {
            return AssertionResult.Success();    
        }
                
        var errorBuilder = new StringBuilder();
        errorBuilder.AppendLine("Query does not match :");
                
        foreach (var difference in differences)
        {
            errorBuilder.AppendLine($"   -> {difference}");
        }
                
        return AssertionResult.Failure(errorBuilder.ToString());
    }
}