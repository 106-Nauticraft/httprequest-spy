#nullable enable
namespace HttpRequestSpy.Assertions;

public interface IAssertHttpRequest
{
    public AssertionResult Matches(AssertableHttpRequestMessage request);
    string ToExpectation();
}