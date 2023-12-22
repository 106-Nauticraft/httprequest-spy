using System.Text.Json;
using HttpRequestSpy.Assertions.HttpRequestAssertions;
using Json.Schema;

namespace HttpRequestSpy.Assertions;

public sealed record HttpRequestWithPayloadAssertion : HttpRequestAssertion
{
    public HttpRequestWithPayloadAssertion(HttpMethod method, string url,
        List<RecordedHttpRequest> recordedHttpRequests) : base(method, url, recordedHttpRequests)
    {
    }

    public HttpRequestWithPayloadAssertion WithJsonPayload<T>(T expectedRequestPayload, JsonSerializerOptions? jsonSerializerOptions = null, bool logRecordedRequestPayload = false)
    {
        return this with
        {
            Assertions = Assertions.Add(JsonPayloadAssertion.MatchesJsonObject(expectedRequestPayload, jsonSerializerOptions, logRecordedRequestPayload))
        };
    }
        
    public HttpRequestWithPayloadAssertion WithJsonPayloadProperty(string expectedPropertyName, object? expectedPropertyValue = default, JsonSerializerOptions? jsonSerializerOptions = null, bool logRecordedRequestPayload = false)
    {
        return this with
        {
            Assertions = Assertions.Add(JsonPayloadAssertion.MatchesJsonProperty(expectedPropertyName, expectedPropertyValue, jsonSerializerOptions, logRecordedRequestPayload))
        };
    }

    public HttpRequestWithPayloadAssertion WithPayloadMatchingJsonSchemaFromText(string schema)
    {
        return WithPayloadMatchingJsonSchema(JsonSchema.FromText(schema));
    }
        
    public HttpRequestWithPayloadAssertion WithPayloadMatchingJsonSchema(string fileName)
    {
        return WithPayloadMatchingJsonSchema(JsonSchema.FromFile(fileName));
    }

    public HttpRequestWithPayloadAssertion WithPayloadMatchingJsonSchema(JsonSchema schema)
    {
        return this with
        {
            Assertions = Assertions.Add(JsonPayloadAssertion.MatchesJsonSchema(schema))
        };
    }

    public HttpRequestWithPayloadAssertion WithXmlPayload(object payload, bool logRecordedRequestPayload = false)
    {
        return this with
        {
            Assertions = Assertions.Add(XmlPayloadAssertion.MatchesObject(payload, logRecordedRequestPayload))
        };
    }
    
    public HttpRequestWithPayloadAssertion WithXmlPayloadProperty(PropertyXPath propertyXPath, object? payload = null, bool logRecordedRequestPayload = false)
    {
        return this with
        {
            Assertions = Assertions.Add(XmlPayloadAssertion.MatchesProperty(propertyXPath, payload, logRecordedRequestPayload))
        };
    }
}

