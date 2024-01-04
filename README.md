# httprequest-spy

`HttpRequestSpy` is a tool aiming to test outgoing HttpRequest sent via an HttpClient.

It compares all sent http requests to an expected request definition and provides a user friendly error message when no recorded httprequest matches the expected one.

This tool is useful when you want to test your outgoing http requests but you don't want to mock the http client.

## Usage

```csharp
    [Fact]
    public async Task HttpRequest_should_be_sent()
    {
        // Arrange
        var spy = new HttpRequestSpy();
        using var httpClient = new HttpClient(new SpyHttpMessageHandler(spy));

        var instance = new TypedHttpClient(httpClient);

        // Act
        await instance.MakeHttpRequest();

        // Assert
        spy.HasRecordedRequests(1);

        spy.AGetRequestTo("/some/ressource")
           .WithQuery(new {
               id = "12"
           })
           .OccuredOnce();
    }


    public class TypedHttpClient(HttpClient httpClient) {
        public async Task<HttpResponseMessage> MakeHttpRequest() {
            return await httpClient.GetAsync("/some/ressource?id=12");
        }
    }
```
