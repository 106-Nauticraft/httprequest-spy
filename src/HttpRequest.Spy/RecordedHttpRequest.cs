namespace HttpRequest.Spy;

public sealed class RecordedHttpRequest
{
    public HttpRequestMessage Request { get; }

    public HttpResponseMessage? Response { get; private set; }

    public static async Task<RecordedHttpRequest> From(HttpRequestMessage request)
    {
        var httpRequestMessage = await request.Clone();
        return new RecordedHttpRequest(httpRequestMessage);
    }

    private RecordedHttpRequest(HttpRequestMessage request)
    {
        Request = request;
    }

    public void WithResponse(HttpResponseMessage response)
    {
        Response = response;
    }

    public override string ToString()
    {
        var request = RequestToString();
        var response = ResponseToString();
        return $"{request}{Environment.NewLine}{response}";
    }

    private string RequestToString()
    {
        return $"REQUEST  : {Request.Method} {Request.RequestUri}";
    }

    private string ResponseToString()
    {
        if (Response is null)
        {
            return "RESPONSE : [NO RESPONSE]";
        }

        return $"RESPONSE : {(int) Response.StatusCode} ({Response.StatusCode}) : {Response.ReasonPhrase} ";
    }
}