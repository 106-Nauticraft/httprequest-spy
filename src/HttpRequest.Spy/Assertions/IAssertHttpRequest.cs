namespace HttpRequest.Spy.Assertions;

public interface IAssertHttpRequest
{
    public AssertionResult Matches(AssertableHttpRequestMessage request);
    string ToExpectation();
}