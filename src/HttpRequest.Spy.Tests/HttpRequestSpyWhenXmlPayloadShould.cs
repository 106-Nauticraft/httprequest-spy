using System.Net.Mime;
using System.ServiceModel.Channels;
using System.Text;
using System.Xml.Serialization;
using HttpRequest.Spy.Assertions.HttpRequestAssertions;
using HttpRequest.Spy.Tests.Soap;
using JetBrains.Annotations;

namespace HttpRequest.Spy.Tests;

public partial class HttpRequestSpyShould
{
    [Fact]
    public async Task Ensure_that_a_post_request_with_anonymous_xml_payload_is_sent()
    {
        var spy = HttpRequestSpy.CreateCurrentSpy();

        await RegisterPostWithXmlPayload(new { something = true, property = 2 });

        spy.APostRequestTo("/path/to/resource")
            .WithXmlPayload(new { something = true, property = 2 })
            .OccurredOnce();
    }

    [Fact]
    public async Task Ensure_that_a_post_request_with_anonymous_xml_payload_containing_some_property_is_sent()
    {
        var spy = HttpRequestSpy.CreateCurrentSpy();

        await RegisterPostWithXmlPayload(new
        {
            something = true,
            property = new
            {
                subProperty = new
                {
                    other = "hello",
                    values = new[] { 1, 2, 3 }
                }
            }
        });

        spy.APostRequestTo("/path/to/resource")
            .WithXmlPayloadProperty("/_x003F_anonymous_x003F_/something")
            .WithXmlPayloadProperty("/_x003F_anonymous_x003F_/property/subProperty", new
            {
                other = "hello",
                values = new[] { 1, 2, 3 }
            })
            .OccurredOnce();
    }



    [Fact]
    public async Task Ensure_that_a_post_request_with_soap_payload()
    {
        var spy = HttpRequestSpy.CreateCurrentSpy();

        var id = Guid.NewGuid();
        var soapMessage = new SoapRequest(new SoapRequestId(id, "crs"), new[] { "item1", "item2" });

        await RegisterSoapRequest(soapMessage);

        spy.APostRequestTo("/path/to/resource")
            .WithXmlPayloadProperty(PropertyXPath.WithNamespaces("/s:Envelope/*[local-name()='Body']/*[local-name()='SoapRequest']"))
            .WithXmlPayloadProperty("/Envelope/Body/SoapRequest/Id/@Type", "crs")
            .WithXmlPayloadProperty("/Envelope/Body/SoapRequest/Id/Value", $"{id}")
            .OccurredOnce();
    }

    [Fact]
    public async Task Ensure_that_a_post_request_with_xml_payload_with_xml_attributes_is_sent()
    {
        var spy = HttpRequestSpy.CreateCurrentSpy();

        await RegisterPostWithXmlPayload(new XmlRequest("Hello", 1));

        spy.APostRequestTo("/path/to/resource")
            .WithXmlPayload(new XmlRequest("Hello", 1))
            .OccurredOnce();
    }

    [Fact]
    public async Task Fails_when_xml_payload_does_not_match_expected_one()
    {
        var spy = HttpRequestSpy.CreateCurrentSpy();

        await RegisterPostWithXmlPayload(new XmlRequest("Hello", 20));

        Check.ThatCode(() =>
                spy.APostRequestTo("/path/to/resource")
                    .WithXmlPayload(new XmlRequest("World", 2))
                    .OccurredOnce())
        .Throws<HttpRequestSpyException>();
    }


    [Fact]
    public async Task Fails_when_xml_payload_property_does_not_exist()
    {
        var spy = HttpRequestSpy.CreateCurrentSpy();

        await RegisterPostWithXmlPayload(new XmlRequest("Hello", 20));

        Check.ThatCode(() =>
                spy.APostRequestTo("/path/to/resource")
                    .WithXmlPayloadProperty("/unknown/@attr")
                    .OccurredOnce())
            .Throws<HttpRequestSpyException>();
    }

    [Fact]
    public async Task Fails_when_xml_payload_property_does_not_match_expected_one()
    {
        var spy = HttpRequestSpy.CreateCurrentSpy();

        await RegisterPostWithXmlPayload(new XmlRequest("Hello", 20));

        Check.ThatCode(() =>
                spy.APostRequestTo("/path/to/resource")
                    .WithXmlPayloadProperty("/@Order", 19)
                    .OccurredOnce())
            .Throws<HttpRequestSpyException>();
    }

    [Fact]
    public async Task Fails_when_actual_payload_is_not_a_xml_as_expected()
    {
        var spy = HttpRequestSpy.CreateCurrentSpy();

        await RegisterPostWithJsonPayloadRequest(new
        {
            prop1 = 1,
            prop2 = "value"
        });

        Check.ThatCode(() =>
                spy.APostRequestTo("/path/to/resource")
                    .WithXmlPayload(new
                    {
                        prop1 = 1,
                        prop2 = "value"
                    })
                    .OccurredOnce())
            .Throws<HttpRequestSpyException>();
    }

    public record XmlRequest([UsedImplicitly] string Value, [property: XmlAttribute] int Order)
    {
        public XmlRequest() : this("", 0) { }
    }

    [XmlType]
    public record SoapRequestId([UsedImplicitly]
        Guid Value,
        [property: XmlAttribute(AttributeName = "Type")] string Type)
    {
        public SoapRequestId() : this(new Guid(), "")
        {
        }
    }

    [XmlType(Namespace = "https://some.namespace.com/2023/08")]
    public record SoapRequest(
        [property:XmlElement]
        SoapRequestId? Id,
        [property: XmlArray][UsedImplicitly] string[] List)
    {
        public SoapRequest() : this(null, Array.Empty<string>()) { }
    }

    private static async Task RegisterSoapRequest<T>(T payload)
        where T : class
    {
        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post,
            $"{AbsoluteRoute}")
        {
            Content = new SoapXmlContent(payload, MessageVersion.Soap11WSAddressingAugust2004)
        };

        var record = await
            RecordedHttpRequest.From(httpRequestMessage);
        HttpRequestSpy.Current?.RecordRequest(record);
    }

    private static async Task RegisterPostWithXmlPayload<T>(T payload)
        where T : class
    {
        var xml = payload.ToXml().ToString();

        var content = new StringContent(xml, Encoding.UTF8, MediaTypeNames.Application.Xml);

        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post,
            $"{AbsoluteRoute}")
        {
            Content = content
        };

        var record = await
            RecordedHttpRequest.From(httpRequestMessage);
        HttpRequestSpy.Current?.RecordRequest(record);
    }
}