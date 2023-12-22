namespace HttpRequestSpy.Assertions.HttpRequestAssertions;
#nullable enable
internal record HttpMethodAssertion(HttpMethod ExpectedHttpMethod) : IAssertHttpRequest
{
    public AssertionResult Matches(AssertableHttpRequestMessage request)
    {
        if (request.InnerRequest.Method.Method != ExpectedHttpMethod.Method)
        {
            return AssertionResult.Failure( 
                "HttpMethod does not match", 
                ExpectedHttpMethod.Method,
                request.InnerRequest.Method.Method
            );
        }

        return AssertionResult.Success();
    }
        
    public string ToExpectation()
    {
        return $"Method : {ExpectedHttpMethod.Method}";
    }
}