using System.Text.Json.Serialization;

namespace HttpRequest.Spy.Tests;

public partial class HttpRequestSpyShould
{
    [Fact]
    public async Task Ensure_that_a_post_request_with_json_payload_is_sent()
    {
        var spy = HttpRequestSpy.CreateCurrentSpy();

        await RegisterPostWithJsonPayloadRequest(new { something = true, property = 2 });

        spy.APostRequestTo("/path/to/resource")
            .WithJsonPayload(new { something = true, property = 2 })
            .OccurredOnce();
    }

    [Fact]
    public async Task Ensure_that_a_post_request_with_json_payload_containing_some_property_is_sent()
    {
        var spy = HttpRequestSpy.CreateCurrentSpy();

        await RegisterPostWithJsonPayloadRequest(new { something = new { active = true }, property = 2 });

        spy.APostRequestTo("/path/to/resource")
            .WithJsonPayloadProperty("something")
            .OccurredOnce();
    }


    [Fact]
    public async Task Ensure_that_a_post_request_with_json_payload_containing_some_property_matching_value_is_sent()
    {
        var spy = HttpRequestSpy.CreateCurrentSpy();

        await RegisterPostWithJsonPayloadRequest(new { something = new { active = true }, property = 2 });

        spy.APostRequestTo("/path/to/resource")
            .WithJsonPayloadProperty("something", new { active = true })
            .OccurredOnce();
    }

    [Fact]
    public async Task Ensure_that_a_post_request_with_payload_matching_a_json_schema_is_sent()
    {
        var spy = HttpRequestSpy.CreateCurrentSpy();

        await RegisterPostWithJsonPayloadRequest(new { something = true, property = 22 });

        var schema =
            @"{
                ""$schema"": ""https://json-schema.org/draft/2020-12/schema"",
                ""$id"": ""https://example.com/product.schema.json"",
                ""title"": ""Test Payload"",
                ""description"": ""A Payload"",
                ""type"": ""object"",
                ""properties"": {
                    ""something"": {
                        ""description"": ""something"",
                        ""type"": ""boolean""
                    },
                    ""property"": {
                        ""description"": ""property"",
                        ""type"": ""integer""
                    }
                },
                ""required"": [ ""something"", ""property"" ]
            }";

        spy.APostRequestTo("/path/to/resource")
            .WithPayloadMatchingJsonSchemaFromText(schema)
            .OccurredOnce();
    }


    [Fact]
    public async Task Deserialize_json_payload()
    {
        var spy = HttpRequestSpy.CreateCurrentSpy();

        await RegisterPostWithJsonPayloadRequest(new JsonRequest("hello"));

        spy.APostRequestTo("/path/to/resource")
            .WithJsonPayload(new JsonRequest("hello"))
            .OccurredOnce();

        Check.ThatCode(() =>
                spy.APostRequestTo("/path/to/resource")
                    .WithJsonPayload(new { property = "hello", nullableProperty = (object?)null }, logRecordedRequestPayload: true)
                    .OccurredOnce())
            .Throws<HttpRequestSpyException>();
    }

    [Fact]
    public async Task Ensure_that_a_patch_request_with_json_payload_is_sent()
    {
        var spy = HttpRequestSpy.CreateCurrentSpy();

        await RegisterRequest(HttpMethod.Patch, payload: new { something = true, property = 2 });

        spy.APatchRequestTo("/path/to/resource")
            .WithJsonPayload(new { something = true, property = 2 })
            .OccurredOnce();
    }

    [Fact]
    public async Task Ensure_that_a_put_request_with_json_payload_is_sent()
    {
        var spy = HttpRequestSpy.CreateCurrentSpy();

        await RegisterRequest(HttpMethod.Put, payload: new { something = true, property = 2 });

        spy.APutRequestTo("/path/to/resource")
            .WithJsonPayload(new { something = true, property = 2 })
            .OccurredOnce();
    }

    [Fact]
    public async Task Fails_when_json_payload_does_not_match_expected_one()
    {
        var spy = HttpRequestSpy.CreateCurrentSpy();

        await RegisterPostWithJsonPayloadRequest(new { something = true, property = 10, other = "" });

        Check.ThatCode(() =>
                spy.APostRequestTo("/path/to/resource")
                    .WithJsonPayload(new { something = true, property = 2 })
                    .OccurredOnce())
            .Throws<HttpRequestSpyException>();
    }

    [Fact]
    public async Task Fails_when_payload_does_not_match_expected_schema()
    {
        var spy = HttpRequestSpy.CreateCurrentSpy();

        await RegisterPostWithJsonPayloadRequest(new { something = true, property = "string", anotherProperty = "string" });

        var schema =
            @"{
                ""$schema"": ""https://json-schema.org/draft/2020-12/schema"",
                ""$id"": ""https://example.com/product.schema.json"",
                ""title"": ""Test Payload"",
                ""description"": ""A Payload"",
                ""type"": ""object"",
                ""properties"": {
                    ""something"": {
                        ""description"": ""something"",
                        ""type"": ""boolean""
                    },
                    ""property"": {
                        ""description"": ""property"",
                        ""type"": ""integer""
                    },
                    ""anotherProperty"": {
                        ""description"": ""property"",
                        ""type"": ""integer""
                    }
                },
                ""required"": [ ""something"", ""property"", ""anotherProperty"" ]
            }";

        Check.ThatCode(() =>
                spy.APostRequestTo("/path/to/resource")
                    .WithPayloadMatchingJsonSchemaFromText(schema)
                    .OccurredOnce())
            .Throws<HttpRequestSpyException>()
            .WithMessage(message => message.Contains("At /properties/property : Value is \"string\" but should be \"integer\""))
            .WithMessage(message => message.Contains("At /properties/anotherProperty : Value is \"string\" but should be \"integer\""));
    }


    [Fact]
    public async Task Fails_when_actual_payload_is_not_a_json_as_expected()
    {
        var spy = HttpRequestSpy.CreateCurrentSpy();

        await HttpRequestSpyShould.RegisterPostWithXmlPayload(new
        {
            prop1 = 1,
            prop2 = "value"
        });

        Check.ThatCode(() =>
                spy.APostRequestTo("/path/to/resource")
                    .WithJsonPayload(new
                    {
                        prop1 = 1,
                        prop2 = "value"
                    })
                    .OccurredOnce())
            .Throws<HttpRequestSpyException>();
    }

    [Fact]
    public async Task Fails_when_json_payload_does_not_contain_expected_property()
    {
        var spy = HttpRequestSpy.CreateCurrentSpy();

        await RegisterPostWithJsonPayloadRequest(new
        {
            prop = 1,
            prop2 = ""
        });

        Check.ThatCode(() =>
                spy.APostRequestTo("/path/to/resource")
                    .WithJsonPayloadProperty("unknown")
                    .OccurredOnce()
            )
            .Throws<HttpRequestSpyException>();
    }

    [Fact]
    public async Task Fails_when_json_payload_contains_expected_property_but_values_do_not_match()
    {
        var spy = HttpRequestSpy.CreateCurrentSpy();

        await RegisterPostWithJsonPayloadRequest(new
        {
            prop = 1,
            prop2 = new
            {
                flag = true
            }
        });

        Check.ThatCode(() =>
            spy.APostRequestTo("/path/to/resource")
                .WithJsonPayloadProperty("prop2", new
                {
                    flag = false
                })
                .OccurredOnce()
        ).Throws<HttpRequestSpyException>();
    }

    private record JsonRequest(
        [property: JsonPropertyName("PROPERTY")] 
        // ReSharper disable once NotAccessedPositionalProperty.Local
        string Property,
        // ReSharper disable once NotAccessedPositionalProperty.Local
        object? NullableProperty = null);

    private static Task RegisterPostWithJsonPayloadRequest(object? payload = null)
    {
        return RegisterRequest(HttpMethod.Post, payload: payload);
    }
}