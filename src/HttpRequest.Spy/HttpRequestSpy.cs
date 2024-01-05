using System.Text;
using HttpRequest.Spy.Assertions;

namespace HttpRequest.Spy;

public sealed class HttpRequestSpy : IDisposable
{
    private static readonly AsyncLocal<HttpRequestSpy?> State = new();
    public static HttpRequestSpy? Current => State.Value;

    public static HttpRequestSpy Create()
    {
        return new HttpRequestSpy(false);
    }

    public static HttpRequestSpy CreateCurrentSpy()
    {
        return new HttpRequestSpy(true);
    }

    private HttpRequestSpy(bool saveInAsyncLocale)
    {
        if (saveInAsyncLocale)
        {
            State.Value = this;
        }
    }

    private readonly List<RecordedHttpRequest> _recordedRequests = new();

    public void RecordRequest(RecordedHttpRequest recordedRequest)
    {
        _recordedRequests.Add(recordedRequest);
    }

    public void HasRecordedRequests(int nbOfRecordedRequests)
    {
        if (_recordedRequests.Count == nbOfRecordedRequests)
            return;

        var messageBuilder = new StringBuilder()
            .AppendLine();

        messageBuilder.AppendLine(
            $"  ⚠ Unexpected number of recorded requests. Expected : {nbOfRecordedRequests}. Actual : {_recordedRequests.Count}");

        throw new HttpRequestSpyException(messageBuilder);
    }

    public HttpRequestAssertion AGetRequestTo(string url)
    {
        return new HttpRequestAssertion(HttpMethod.Get, url, _recordedRequests);
    }

    public HttpRequestWithPayloadAssertion APostRequestTo(string url)
    {
        return new HttpRequestWithPayloadAssertion(HttpMethod.Post, url, _recordedRequests);
    }

    public HttpRequestWithPayloadAssertion APatchRequestTo(string url)
    {
        return new HttpRequestWithPayloadAssertion(HttpMethod.Patch, url, _recordedRequests);
    }

    public HttpRequestWithPayloadAssertion APutRequestTo(string url)
    {
        return new HttpRequestWithPayloadAssertion(HttpMethod.Put, url, _recordedRequests);
    }

    public HttpRequestAssertion ADeleteRequestTo(string url)
    {
        return new HttpRequestAssertion(HttpMethod.Delete, url, _recordedRequests);
    }

    public void Dispose()
    {
        if (State.Value == this)
        {
            State.Value = null;
        }
    }
}