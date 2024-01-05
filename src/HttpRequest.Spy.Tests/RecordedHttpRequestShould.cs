using System.Net.Http.Json;

namespace HttpRequest.Spy.Tests;

public class RecordedHttpRequestShould
{
    [Fact]
    public async Task Record_a_clone_of_an_HttpRequestMessage_which_content_can_be_read_multiple_times()
    {
        var payload = JsonContent.Create(new { Property = "P" });

        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, "/some/resource")
        {
            Content = payload,
        };

        var record = await RecordedHttpRequest.From(httpRequestMessage);

        Check.That(record.Request).Not.IsSameReferenceAs(httpRequestMessage);
        Check.That(record.Request.Content).Not.IsSameReferenceAs(httpRequestMessage.Content);
    }
}