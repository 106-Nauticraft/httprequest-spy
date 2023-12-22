namespace HttpRequestSpy;

public class SpyHttpMessageHandler : DelegatingHandler
{
    private readonly HttpRequestSpy? _httpRequestSpy;

    private HttpRequestSpy? CurrentHttpRequestSpy => _httpRequestSpy ?? HttpRequestSpy.Current; 
        
    public SpyHttpMessageHandler() { }
        
    public SpyHttpMessageHandler(HttpMessageHandler nextHandler) : base(nextHandler) { }
        
    public SpyHttpMessageHandler(HttpRequestSpy httpRequestSpy, HttpMessageHandler nextHandler) : base(nextHandler)
    {
        _httpRequestSpy = httpRequestSpy;
    }
        
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var record = await RecordedHttpRequest.From(request);
            
        CurrentHttpRequestSpy?.RecordRequest(record);
            
        var response = await base.SendAsync(request, cancellationToken);

        record.WithResponse(response);
            
        return response;
    }

    protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var record = RecordedHttpRequest.From(request).GetAwaiter().GetResult();
            
        CurrentHttpRequestSpy?.RecordRequest(record);
            
        var response = base.Send(request, cancellationToken);

        record.WithResponse(response);
            
        return response;
    }
}