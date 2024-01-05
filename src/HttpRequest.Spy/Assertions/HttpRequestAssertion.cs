using System.Collections.Immutable;
using System.Text;
using HttpRequest.Spy.Assertions.HttpRequestAssertions;

namespace HttpRequest.Spy.Assertions;

public record HttpRequestAssertion
{
    protected ImmutableList<IAssertHttpRequest> Assertions { get; init; } =
        ImmutableList<IAssertHttpRequest>.Empty;

    private readonly List<RecordedHttpRequest> _recordedHttpRequests;

    public HttpRequestAssertion(HttpMethod httpMethod, string url,
        List<RecordedHttpRequest> recordedHttpRequests)
    {
        Assertions = Assertions
            .Add(new HttpMethodAssertion(httpMethod))
            .Add(UrlAssertion.From(url));
        _recordedHttpRequests = recordedHttpRequests;
    }

    public HttpRequestAssertion WithQuery(string query)
    {
        return this with
        {
            Assertions = Assertions.Add(QueryAssertion.FromString(query))
        };
    }
        
    public HttpRequestAssertion WithQuery(object query)
    {
        return this with
        {
            Assertions = Assertions.Add(QueryAssertion.FromObject(query))
        };
    }

    public HttpRequestAssertion WithQueryParam(string expectedQueryParamName, string? expectedQueryParamValue = null)
    {
        return this with
        {
            Assertions = Assertions.Add(QueryAssertion.SingleParam(expectedQueryParamName, expectedQueryParamValue))
        };
    }
        
    public void OccurredOnce()
    {
        Occurred(1);
    }

    public void OccurredTwice()
    {
        Occurred(2);
    }

    public void NeverOccurred()
    {
        Occurred(0);
    }

    private IReadOnlyCollection<(RecordedHttpRequest Request, AssertionResult Result)> AssertHttpRequests()
    {
        return _recordedHttpRequests.Select(request => (request, AssertHttpRequest(request))).ToList();
    }

    private AssertionResult AssertHttpRequest(RecordedHttpRequest request)
    {
        return Assertions
            .Select(assert => assert.Matches(new AssertableHttpRequestMessage(request.Request)))
            .Aggregate((res1, res2) => res1.And(res2));
    }

    private void WriteExpectations(StringBuilder messageBuilder)
    {
        messageBuilder.AppendLine();
        messageBuilder.AppendLine("Expectations: ");
            
        Assertions.ForEach(assertion =>
        {
            messageBuilder.AppendLine($" ✔ {assertion.ToExpectation()}");
        });
    }

    public void Occurred(int nbOfRequests)
    {
        var requestWithAssertionResult = AssertHttpRequests();

        var matchingRequest = requestWithAssertionResult
            .Where(t => t.Item2 is AssertionResult.SuccessResult).ToList();
            
        if (matchingRequest.Count == nbOfRequests)
        {
            return;
        }
            
        if (!_recordedHttpRequests.Any())
        {
            throw new HttpRequestSpyException(" ⚠ No Http request recorded.");
        }

        var messageBuilder = new StringBuilder()
            .AppendLine();

        messageBuilder.AppendLine(
            $"  ⚠ Unexpected number of recorded requests matching expectations. Expected : {nbOfRequests}. Actual : {matchingRequest.Count}");

        WriteExpectations(messageBuilder);
        WriteRecordedRequests(requestWithAssertionResult, messageBuilder);

        throw new HttpRequestSpyException(messageBuilder);
    }

    private static void WriteRecordedRequests(
        IReadOnlyCollection<(RecordedHttpRequest Request, AssertionResult Result)> requestsWithAssertionResult, StringBuilder messageBuilder)
    {
        messageBuilder.AppendLine();
        messageBuilder.AppendLine($"{requestsWithAssertionResult.Count} Recorded requests: ");

        var index = 0;

        foreach (var (record, assertionResult) in requestsWithAssertionResult)
        {
            messageBuilder.AppendLine($"{index}/ {record.Request.Method.Method} {record.Request.RequestUri}");
                
            if (assertionResult is AssertionResult.FailureResult failure)
            {
                foreach (var difference in failure.ComparisonResult)
                {
                    messageBuilder.AppendLine($"  ⚠ {difference}");
                }
            }
                
            index++;
        }   
    }
}