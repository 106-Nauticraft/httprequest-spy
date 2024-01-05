#nullable enable
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Json.Schema;

namespace HttpRequest.Spy.Assertions.HttpRequestAssertions;

internal abstract record JsonPayloadAssertion : IAssertHttpRequest
{
        
    private record MatchedJsonSchemaAssertion(JsonSchema JsonSchema) : JsonPayloadAssertion
    {
        public override AssertionResult Matches(AssertableHttpRequestMessage request)
        {
            var content = request.InnerRequest.Content;
            if (content is null)
            {
                return AssertionResult.Success();
            }

            var stream = request.RewindableContentStream!;
            stream.Seek(0, SeekOrigin.Begin);
            var jsonDocument = JsonDocument.Parse(stream);

            var validation = JsonSchema.Validate(jsonDocument, new ValidationOptions
            {
                OutputFormat = OutputFormat.Detailed
            });
                
            if (!validation.IsValid)
            {
                return AssertionResult.Failure($"Json Schema does not match : {validation.Message}");
            }
                
            return AssertionResult.Success();
        }

        public override string ToExpectation()
        {
            return $"JsonSchema : {JsonSchema}";
        }
    }
        
    public static JsonPayloadAssertion MatchesJsonSchema(JsonSchema jsonSchema) => new MatchedJsonSchemaAssertion(jsonSchema);

    
    
    private record MatchesJsonObjectAssertion<T>(T ExpectedJsonObject, JsonSerializerOptions? JsonSerializerOptions,
        bool LogRecordedRequestPayload) : JsonPayloadAssertion
    {
        public override AssertionResult Matches(AssertableHttpRequestMessage request)
        {
            var content = request.InnerRequest.Content;
                
            if (content is null)
            {
                return 
                    AssertionResult.Failure(
                        "Payload is empty", 
                        ExpectedPayloadAsJson.ToString(),
                        "[null]"
                    );
            }
                
            if (content.Headers.ContentType?.MediaType is not System.Net.Mime.MediaTypeNames.Application.Json)
            {
                return 
                    AssertionResult.Failure(
                        "Payload type does not match", 
                        System.Net.Mime.MediaTypeNames.Application.Json,
                        content.Headers.ContentType?.MediaType
                    );
            }
                
            var actualPayload = ReadJson(request.RewindableContentStream!);

            var differences = actualPayload.CompareTo(ExpectedPayloadAsJson);

            if (differences.IsEmpty)
            {
                return AssertionResult.Success();
            }

            var errorBuilder = new StringBuilder();
                    
            errorBuilder.AppendLine("Payload does not match :");

            if (LogRecordedRequestPayload)
            {
                errorBuilder.AppendLine(actualPayload.ToString());
            }

            foreach (var difference in differences)
            {
                errorBuilder.AppendLine($"   -> {difference}");
            }
                    
            return AssertionResult.Failure(errorBuilder.ToString());
        }

        public override string ToExpectation()
        {
            return $"Payload : {ExpectedPayloadAsJson}";
        }

        private static JsonElement ReadJson(Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);
            return JsonDocument.Parse(stream).RootElement;
        }

        private JsonElement ExpectedPayloadAsJson
        {
            get
            {
                // Yes we could deserialize using System.Text.Json directly but we want to have the same serialization options as the HttpContent of the checked httprequest (which should be serialized as JsonContent)
                var content = JsonContent.Create(ExpectedJsonObject, null, JsonSerializerOptions);
                return ReadJson(content.ReadAsStream());
            }
        } 
            
    }
    
    public static JsonPayloadAssertion MatchesJsonObject<T>(T expectedPayload,
        JsonSerializerOptions? jsonSerializerOptions, bool logRecordedRequestPayload) =>
        new MatchesJsonObjectAssertion<T>(expectedPayload, jsonSerializerOptions, logRecordedRequestPayload);

    
    
    private record PropertyMatchesJsonObjectAssertion<T>(string ExpectedPropertyName, T? ExpectedPropertyValue, JsonSerializerOptions? JsonSerializerOptions, bool LogRecordedRequestPayload) : JsonPayloadAssertion
    {
        public override AssertionResult Matches(AssertableHttpRequestMessage request)
        {
            var content = request.InnerRequest.Content;
                
            if (content is null)
            {
                var expectedPropertyPayload = GetExpectedPartialPayload();

                return 
                    AssertionResult.Failure(
                        "Payload is empty", 
                        expectedPropertyPayload,
                        "[null]"
                    );
            }
                
            if (content.Headers.ContentType?.MediaType is not System.Net.Mime.MediaTypeNames.Application.Json)
            {
                return 
                    AssertionResult.Failure(
                        "Payload type does not match", 
                        System.Net.Mime.MediaTypeNames.Application.Json,
                        content.Headers.ContentType?.MediaType
                    );
            }
                
            var actualPayload = ReadJson(request.RewindableContentStream!);

            try
            {
                var actualPropertyValue = actualPayload.GetProperty(ExpectedPropertyName);

                if (!CheckPropertyValue)
                {
                    return AssertionResult.Success();
                }
                    
                var differences = actualPropertyValue.CompareTo(ExpectedPropertyValueAsJson);

                if (differences.IsEmpty)
                {
                    return AssertionResult.Success();
                }

                var errorBuilder = new StringBuilder();

                errorBuilder.AppendLine($"Property {ExpectedPropertyName} in json payload does not match :");
                    
                if (LogRecordedRequestPayload)
                {
                    errorBuilder.AppendLine(actualPropertyValue.ToString());
                }

                foreach (var difference in differences)
                {
                    errorBuilder.AppendLine($"   -> {difference}");
                }

                return AssertionResult.Failure(errorBuilder.ToString());
            }
            catch (KeyNotFoundException)
            {
                return AssertionResult.Failure($"Property {ExpectedPropertyName} was not found in json payload", GetExpectedPartialPayload(), actualPayload.ToString());
            }
        }

        private string GetExpectedPartialPayload()
        {
            var expectedPropertyValue = CheckPropertyValue ? ExpectedPropertyValueAsJson.ToString() : " { /* */ }";

            var expectedPropertyPayload =
                $"Payload containing {{ ... {ExpectedPropertyName}:{expectedPropertyValue} ... }}";
            return expectedPropertyPayload;
        }

        public override string ToExpectation()
        {
            return GetExpectedPartialPayload();
        }

        private static JsonElement ReadJson(Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);
            return JsonDocument.Parse(stream).RootElement;
        }

        private bool CheckPropertyValue => ExpectedPropertyValue is not null;
            
        private JsonElement ExpectedPropertyValueAsJson
        {
            get
            {
                // Yes we could deserialize using System.Text.Json directly but we want to have the same serialization options as the HttpContent of the checked httprequest (which should be serialized as JsonContent)
                var content = JsonContent.Create(ExpectedPropertyValue, null, JsonSerializerOptions);
                return ReadJson(content.ReadAsStream());
            }
        }
    }
    
    public static JsonPayloadAssertion MatchesJsonProperty<T>(string expectedPropertyName, T? expectedPayloadPropertyValue,
        JsonSerializerOptions? jsonSerializerOptions, bool logRecordedRequestPayload) =>
        new PropertyMatchesJsonObjectAssertion<T>(expectedPropertyName, expectedPayloadPropertyValue, jsonSerializerOptions, logRecordedRequestPayload);
    
    
    public abstract AssertionResult Matches(AssertableHttpRequestMessage request);

    public abstract string ToExpectation();
}