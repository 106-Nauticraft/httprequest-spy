using System.Xml.Linq;
using HttpRequest.Spy.Comparison;

#nullable enable

// ReSharper disable once CheckNamespace
namespace System.Net.Http;

public static partial class HttpContentExtensionMethod
{
    public static async Task<ComparisonResult> CompareContentAsXmlTo(this HttpContent content, object expected,
        IReadOnlyCollection<string>? excludedPaths = null)
    {
        return await content.CompareContentAsXmlTo(expected.ToXml(), options => 
            excludedPaths is not null 
                ? options.Exclude(excludedPaths.ToArray()) 
                : options);
    }

    public static async Task<ComparisonResult> CompareContentAsXmlTo(this HttpContent httpContent, XElement expected,
        Func<ComparisonOptions, ComparisonOptions>? configure = null) 

    {
        var contentAsStream = await httpContent.ReadAsStreamAsync();
        var actual = XElement.Load(contentAsStream);
        return actual.CompareTo(expected, configure);
    }
}