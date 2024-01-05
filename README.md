# HttpRequestSpy

![example workflow](https://github.com/benoit-maurice/httprequest-spy/actions/workflows/main.yml/badge.svg)

`HttpRequestSpy` is a tool aiming to test outgoing HttpRequest sent via an HttpClient.

It compares all sent http requests to an expected request definition and provides a user friendly error message when no recorded httprequest matches the expected one.

This tool is useful when you want to test your outgoing http requests but you don't want to mock the http client.

## Installation 

``dotnet add package HttpRequestSpy``

## Usage

```csharp
    [Fact]
    public async Task HttpRequest_should_be_sent()
    {
        // Arrange
        var spy = HttpRequestSpy.Create();
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

## Features

### Fluent API

The `HttpRequestSpy` provides a fluent API to help you define your expected http request.

```csharp
    spy.AGetRequestTo("/some/ressource")
       .WithQuery(new {
           id = "12"
       })
       .OccuredOnce();
```

### Occurrences

You can define the number of occurences of your expected request.

```csharp
    spy.AGetRequestTo("/some/ressource")
       .OccuredOnce();

    spy.AGetRequestTo("/some/ressource")
       .OccuredTwice();

    spy.AGetRequestTo("/some/ressource")
       .Occured(3);
    
    spy.AGetRequestTo("/some/ressource")
       .NeverOccured();
```

### Number of overall recorded requests

You can define the number of overall recorded requests.

```csharp
    spy.HasRecordedRequests(5);
       .NeverOccured();
```
This codes checks that 5 requests have been recorded regardless of the content of the requests.

### Absolute path and relative url

You can define the absolute path or the relative url of your expected request.

```csharp
    spy.AGetRequestTo("/some/ressource")
       .OccuredOnce();
```

```csharp
    spy.AGetRequestTo("https://myapi.com/some/ressource")
       .OccuredOnce();
```

### GET, POST, PUT, DELETE, PATCH http verbs

You can define the http verb of your expected request.

```csharp
    spy.AGetRequestTo("/some/ressource")
       .OccuredOnce();
    
    spy.APostRequestTo("/some/ressource")
       .OccuredOnce();
    
    spy.APutRequestTo("/some/ressource")
       .OccuredOnce();
    
    spy.APatchRequestTo("/some/ressource")
       .OccuredOnce();
    
    spy.ADeleteRequestTo("/some/ressource")
       .OccuredOnce();
```

### Query parameters

You can define the query parameters of your expected request as an anonymous object.

```csharp
    spy.AGetRequestTo("/some/ressource")
       .WithQuery(new {
           id = "12",
           date = "2024-01-05"
       })
        /// Or
       .WithQuery("id=12&date=2024-01-05")
        /// Or
       .WithQueryParam("id", "12")
       .WithQueryParam("date", "2024-01-05")
        
       .OccuredOnce();
```

### Body / Payload

**Only for POST, PUT, PATCH requests**

You can define the body of your expected request as an anonymous object or target a specific property.


#### Check Json payload

See [HttpRequestSpyWhenJsonPayloadShould.cs](./src/HttpRequestSpy.Tests/HttpRequestSpyWhenJsonPayloadShould.cs) file for all use cases.

```csharp
    spy.APostRequestTo("/some/ressource")       
       .WithJsonPayload(new
                    {
                        prop1 = 1,
                        prop2 = "value",
                        subProp = new
                        {
                            subProp1 = "subValue"
                        }
                    })
        
        // Or
        .WithJsonPayloadProperty("prop1") // Will check that the property exists
        .WithJsonPayloadProperty("prop1", 1)
        .WithJsonPayloadProperty("prop2", "value")
        .WithJsonPayloadProperty("subProp", new
                        {
                            subProp1 = "subValue"
                        })
        
        
       .OccuredOnce();
```

##### Json payload matching JsonSchema

Using JsonSchema, you can check that the payload matches a specific schema.

```csharp
    spy.APostRequestTo("/some/ressource")       
        .WithPayloadMatchingJsonSchema("path/to/schema.json")
       .OccuredOnce();
```


#### Check Xml payload

See [HttpRequestSpyWhenXmlPayloadShould.cs](./src/HttpRequestSpy.Tests/HttpRequestSpyWhenXmlPayloadShould.cs) file for all use cases.

```csharp
    spy.APostRequestTo("/some/ressource")
       .WithXmlPayload(new
                    {
                        prop1 = 1,
                        prop2 = "value",
                        subProp = new
                        {
                            subProp1 = "subValue"
                        }
                    })
        
        // Or
        .WithXmlPayloadProperty("/_x003F_anonymous_x003F_/prop1") // Will check that the property exists
        .WithXmlPayloadProperty("/_x003F_anonymous_x003F_/prop1", 1)
        .WithXmlPayloadProperty("/_x003F_anonymous_x003F_/prop2", "value")
        .WithXmlPayloadProperty("/_x003F_anonymous_x003F_/subProp", new
                        {
                            subProp1 = "subValue"
                        })
        // Or  :
        .WithXmlPayloadProperty("/_x003F_anonymous_x003F_/subProp/subProp1", "subValue")
        
        
       .OccuredOnce();
```