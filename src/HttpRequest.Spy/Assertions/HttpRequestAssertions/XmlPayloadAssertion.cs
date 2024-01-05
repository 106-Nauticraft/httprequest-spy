#nullable enable
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using HttpRequest.Spy.Comparison;

namespace HttpRequest.Spy.Assertions.HttpRequestAssertions;

internal abstract record XmlPayloadAssertion : IAssertHttpRequest
{
    
    private record MatchesObjectAssertion(object ExpectedObject, bool LogRecordedRequestPayload) : XmlPayloadAssertion
    {
        public override AssertionResult Matches(AssertableHttpRequestMessage request)
        {
            var result = base.Matches(request);

            if (result is AssertionResult.FailureResult)
            {
                return result;
            }
                
            var actualPayload = ReadXml(request.RewindableContentStream!);

            var differences = actualPayload.CompareTo(ExpectedObject.ToXml());

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
            return $"Payload : {ExpectedObject.ToXml()}";
        }
    }
    public static XmlPayloadAssertion MatchesObject(object expectedPayload, bool logRecordedRequestPayload) =>
        new MatchesObjectAssertion(expectedPayload, logRecordedRequestPayload);
    
    private record PropertyMatchesObjectAssertion(PropertyXPath ExpectedPropertyXPath, object? ExpectedPropertyValue, bool LogRecordedRequestPayload) : XmlPayloadAssertion
    {
        private string ExpectedPropertyName => ExpectedPropertyXPath.PropertyName;
        
        private XElement? _expectedPropertyValueAsXml;
        private XElement? ExpectedPropertyValueAsXml => _expectedPropertyValueAsXml ??= ExpectedPropertyValue?.ToXml(ExpectedPropertyName);
        
        public override AssertionResult Matches(AssertableHttpRequestMessage request)
        {
            var result = base.Matches(request);

            if (result is AssertionResult.FailureResult)
            {
                return result;
            }

            var actualPayload = new XDocument(ReadXml(request.RewindableContentStream!));

            var actualPropertyValue = FindProperty(actualPayload);
            
            if (actualPropertyValue is IEnumerable<object> nodes && !nodes.Any())
            {
                return AssertionResult.Failure($"Property {ExpectedPropertyXPath} was not found in xml payload", ToExpectation(), actualPayload.ToString());
            }

            if (!CheckPropertyValue)
            {
                return AssertionResult.Success();
            }

            var differences = ComparePropertyValueAsXElements(actualPropertyValue, ExpectedPropertyXPath);

            if (differences.IsEmpty)
            {
                return AssertionResult.Success();
            }

            var errorBuilder = new StringBuilder();

            errorBuilder.AppendLine($"Property at {ExpectedPropertyXPath} in xml payload does not match :");
                
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

        private object FindProperty(XDocument actualPayload)  
        {
            if (!ExpectedPropertyXPath.CheckNamespaces)
            {
                return actualPayload.XPathEvaluate(ExpectedPropertyXPath.XPath);
            }

            var namespaceManager = BuildNamespaceManager(actualPayload);
            return actualPayload.XPathEvaluate(ExpectedPropertyXPath.XPath, namespaceManager);
        }


        private static XmlNamespaceManager BuildNamespaceManager(XDocument document)
        {
            var nsManager = new XmlNamespaceManager(new NameTable());

            foreach (var att in document.Root!.DescendantsAndSelf().Attributes())
            {
                if (!att.IsNamespaceDeclaration)
                {
                    continue;
                }
                
                var prefix = att.Name.LocalName == "xmlns" ? string.Empty : att.Name.LocalName;
                
                if (string.IsNullOrWhiteSpace(prefix))
                {
                    continue;
                }
                
                nsManager.AddNamespace(prefix, att.Value);
            }

            return nsManager;
        }
        
        private ComparisonResult ComparePropertyValueAsXElements(object actualPropertyValue, string path)
        {
            switch (actualPropertyValue)
            {
                case IEnumerable<object> nodes:
                    return
                        nodes.Select(element => ComparePropertyValueAsXElements(element, path))
                            .Aggregate((result1, result2) => result1 + result2);
                case XElement {HasElements: true} actualPropertyXElementValue:
                    // Removing the Element from the xml tree
                    actualPropertyXElementValue.Remove();
                    return actualPropertyXElementValue.CompareTo(ExpectedPropertyValueAsXml);
                case XElement {HasElements: false} actualPropertyXElementValue:
                    // Removing the Element from the xml tree
                    actualPropertyXElementValue.Remove();
                    if (actualPropertyXElementValue.Value != ExpectedPropertyValueAsXml?.Value)
                    {
                        return new ComparisonResult.Difference($"Values are different at {path}", ExpectedPropertyValueAsXml?.Value, actualPropertyXElementValue.Value);
                    }
                    return ComparisonResult.Empty; 
                case XAttribute actualAttribute:
                    if (actualAttribute.Value != ExpectedPropertyValueAsXml?.Value)
                    {
                        return new ComparisonResult.Difference($"Values are different at {path}", ExpectedPropertyValueAsXml?.Value, actualAttribute.Value);
                    }
                    
                    break;
            }

            return ComparisonResult.Empty;
        }

        public override string ToExpectation()
        {
            var expectedValue = ExpectedPropertyXPath.PropertyIsAttribute
                ? ExpectedPropertyValue
                : ExpectedPropertyValueAsXml;
            
            var expectedValueCheck = CheckPropertyValue ? $" equal to {expectedValue}" : string.Empty;
            return $"Payload element at {ExpectedPropertyXPath}{expectedValueCheck}";
        }

        private bool CheckPropertyValue => ExpectedPropertyValue is not null;
    }

    public static XmlPayloadAssertion MatchesProperty(PropertyXPath expectedPropertyXPath, object? expectedPayloadPropertyValue, bool logRecordedRequestPayload) =>
        new PropertyMatchesObjectAssertion(expectedPropertyXPath, expectedPayloadPropertyValue, logRecordedRequestPayload);

    
    private static XElement ReadXml(Stream stream)
    {
        stream.Seek(0, SeekOrigin.Begin);
        return XElement.Load(stream);
    }
    
    public virtual AssertionResult Matches(AssertableHttpRequestMessage request)
    {
        var content = request.InnerRequest.Content;
        if (content is null)
        {
            return 
                AssertionResult.Failure(
                    "Payload is empty", 
                    ToExpectation(),
                    "[null]"
                );
        }
                
        if (content.Headers.ContentType?.MediaType is not System.Net.Mime.MediaTypeNames.Application.Xml
            and not System.Net.Mime.MediaTypeNames.Text.Xml)
        {
            return 
                AssertionResult.Failure(
                    "Payload type does not match", 
                    $"{System.Net.Mime.MediaTypeNames.Application.Xml} or {System.Net.Mime.MediaTypeNames.Text.Xml}",
                    content.Headers.ContentType?.MediaType
                );
        }

        return AssertionResult.Success();
    }

    public abstract string ToExpectation();
}

public class PropertyXPath
{
    private const char XPathSeparator = '/';
    private readonly string _rawXPath;
    private readonly string[] _components; 
    public string XPath { get; }
    public string PropertyName { get; }
    public bool PropertyIsAttribute { get; }
    public bool CheckNamespaces { get; }

    private PropertyXPath(string xPath, bool checkNamespaces)
    {
        _rawXPath = xPath;
        CheckNamespaces = checkNamespaces;
        _components = xPath.Split(XPathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        PropertyName = ParseName(_components.Last());
        PropertyIsAttribute = IsAttribute(_components.Last());
        XPath = Adapt(xPath, checkNamespaces);
    }

    private static string ParseName(string component)
    {
        if (IsLocalName(component))
        {
            const string localNameKeyWord = "local-name()=";
            var localNameKeywordIndex = component.IndexOf(localNameKeyWord, StringComparison.Ordinal);
            var localNameKeywordEndIndex = localNameKeywordIndex + localNameKeyWord.Length;
            
            var endOfIndex = component.IndexOf("]", StringComparison.Ordinal);

            var name = component[localNameKeywordEndIndex..endOfIndex]
                .Trim('\'')
                .Trim('"');

            return name;
        }

        return component;
    }

    public static PropertyXPath WithNamespaces(string xPath)
    {
        return new PropertyXPath(xPath, true);
    }

    private string Adapt(string xPath, bool keepNamespaces)
    {
        if (keepNamespaces)
        {
            return xPath;
        }

        var result = "";

        foreach (var component in _components)
        {
            if (string.IsNullOrEmpty(component))
            {
                continue;
            }

            if (IsLocalName(component))
            {
                result += XPathSeparator + component;
                continue;
            }
            
            if (IsAttribute(component))
            {
                result += XPathSeparator + "@" + "*[local-name()='" + IgnoreNamespace(component[1..]) + "']";
            }
            else
            {
                result += XPathSeparator + "*[local-name()='" + IgnoreNamespace(component) + "']";
            }
        }

        return result;
    }

    private static string IgnoreNamespace(string component)
    {
        if (!component.Contains(':'))
        {
            return component;
        }
                
        return component.Split(':').Last();
    }
    
    private static bool IsAttribute(string component)
    {
        return component.StartsWith("@");
    }

    private static bool IsLocalName(string component)
    {
        return component.Contains("*[local-name()=");
    }

    public static PropertyXPath operator +(PropertyXPath xPathLeft, PropertyXPath xPathRight)
    {
        if (xPathLeft.CheckNamespaces != xPathRight.CheckNamespaces)
        {
            throw new Exception("");
        }

        var path = xPathLeft._rawXPath + xPathRight._rawXPath;

        return new PropertyXPath(path, xPathLeft.CheckNamespaces);
    }
    
    public static implicit operator string(PropertyXPath xPath)
    {
        return xPath.XPath;
    }
        
    public static implicit operator PropertyXPath(string xPath)
    {
        return new PropertyXPath(xPath, false);
    }
        
    public override string ToString()
    {
        return _rawXPath;
    }

    public bool StartsWith(PropertyXPath xPath)
    {
        return _rawXPath.StartsWith(xPath._rawXPath);
    }
}