namespace HttpRequestSpy.Assertions;

public class AssertableHttpRequestMessage(HttpRequestMessage innerRequest)
{
    public HttpRequestMessage InnerRequest { get; } = innerRequest;

    private Stream? _rewindableContentStream;
    public Stream? RewindableContentStream => _rewindableContentStream ??= GetRewindableContentStream();

    private Stream? GetRewindableContentStream()
    {
        if (InnerRequest.Content is null)
        {
            return null;
        }
            
        var memoryStream = new MemoryStream();
        InnerRequest.Content.CopyTo(memoryStream, null, CancellationToken.None);
        return memoryStream;
    }
}