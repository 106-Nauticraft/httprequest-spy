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
           .OccurredOnce();
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
       .OccurredOnce();
```

### User friendly error messages

One of the main idea behind this tool is to provide a user friendly error message when no recorded http request matches the expected one.

```csharp
    spy.APostRequestTo("/some/resource")
        .WithJsonPayload(new {
            prop1 = 1
        })
        .WithQuery(new {
            id = "12"
        })
        .OccurredOnce();
```

```text
 ⚠ Unexpected number of recorded requests matching expectations. Expected : 1. Actual : 0

Expectations: 
 ✔ Method : POST
 ✔ Expected URL : /some/resource
 ✔ Payload : {"prop1":1}
 ✔ Query { id = 12 }

2 Recorded requests: 
0/ GET http://domain/path/to/resource
  ⚠ HttpMethod does not match:
 - Expected : POST
 - Actual : GET
  ⚠ URL does not match:
 - Expected : /some/resource
 - Actual : /path/to/resource
  ⚠ Payload is empty:
 - Expected : {"prop1":1}
 - Actual : [null]
  ⚠ Query does not match :
   -> Missing property $.id

1/ POST http://domain/path/to/resource
  ⚠ URL does not match:
 - Expected : /some/resource
 - Actual : /path/to/resource
  ⚠ Payload is empty:
 - Expected : {"prop1":1}
 - Actual : [null]
  ⚠ Query does not match :
   -> Missing property $.id

```



### Occurrences

You can define the number of occurences of your expected request.

```csharp
    spy.AGetRequestTo("/some/ressource")
       .OccurredOnce();

    spy.AGetRequestTo("/some/ressource")
       .OccurredTwice();

    spy.AGetRequestTo("/some/ressource")
       .Occurred(3);
    
    spy.AGetRequestTo("/some/ressource")
       .NeverOccurred();
```

### Number of overall recorded requests

You can define the number of overall recorded requests.

```csharp
    spy.HasRecordedRequests(5);
       .NeverOccurred();
```
This codes checks that 5 requests have been recorded regardless of the content of the requests.

### Absolute path and relative url

You can define the absolute path or the relative url of your expected request.

```csharp
    spy.AGetRequestTo("/some/ressource")
       .OccurredOnce();
```

```csharp
    spy.AGetRequestTo("https://myapi.com/some/ressource")
       .OccurredOnce();
```

### GET, POST, PUT, DELETE, PATCH http verbs

You can define the http verb of your expected request.

```csharp
    spy.AGetRequestTo("/some/ressource")
       .OccurredOnce();
    
    spy.APostRequestTo("/some/ressource")
       .OccurredOnce();
    
    spy.APutRequestTo("/some/ressource")
       .OccurredOnce();
    
    spy.APatchRequestTo("/some/ressource")
       .OccurredOnce();
    
    spy.ADeleteRequestTo("/some/ressource")
       .OccurredOnce();
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
        
       .OccurredOnce();
```

### Body / Payload

**Only for POST, PUT, PATCH requests**

You can define the body of your expected request as an anonymous object or target a specific property.

> Note: payloads are compared using a custom comparison tool that will do a deep equal and returns all the mismatching properties. See the [Comparison](src/HttpRequestSpy.Tests/Comparison) folder. 

> Note 2 : By default, in the error message the payload of the recorded request won't be displayed because it would create a too long message. We only display the mismatching properties. Sometimes, for debugging purpose, you might want to display the full payload. To do so, you can pass `true` to the `logRecordedRequestPayload` parameter.

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
        
        
       .OccurredOnce();
```

> Note 1 : To compare payloads we use System.Text.Json. It is possible to pass a custom JsonSerializerOptions to the WithJsonPayloadProperty and WithJsonPayload methods. 

> Note 2: We're not considering using Newtonsoft.Json at all and it is not possible to change the serializer.

##### Json payload matching JsonSchema

Using JsonSchema, you can check that the payload matches a specific schema.

```csharp
    spy.APostRequestTo("/some/ressource")       
        .WithPayloadMatchingJsonSchema("path/to/schema.json")
       .OccurredOnce();
```

> Note: it is also possible to specify the schema as a string.

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
        
        
       .OccurredOnce();
```

> Note 1: the xpath used is a simplified xpath. It is not a full xpath implementation. It ignores namespaces and attributes for readability purpose. We do not need such complex features. 

> Note 2: in a future version, we might ignore the type node for anonymous objects.

> Note 3: XSD validation is not supported yet.