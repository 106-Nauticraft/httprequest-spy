using System.Xml.XPath;
using HttpRequestSpy.Comparison;

// ReSharper disable once CheckNamespace
namespace System.Xml.Linq;

public static class XElementExtensions
{
    public static ComparisonResult CompareTo(this XElement? actual, XElement? expected, Func<ComparisonOptions, ComparisonOptions>? configure = null)
    {
        var options = configure?.Invoke(new ComparisonOptions()) ?? new ComparisonOptions();

        var rootPath = options.RootPath is not null ? XPath.Root.Append(options.RootPath) : XPath.Root;
        
        return Compare(XmlNode.FromRoot(actual)?.Find(rootPath), XmlNode.FromRoot(expected)?.Find(rootPath), rootPath, options);
    }

    private static ComparisonResult Compare(XmlNode? actual, XmlNode? expected, XPath currentXPath, ComparisonOptions options)
    {
        if (options.IsExcluded(currentXPath))
            return ComparisonResult.Empty;

        if (actual is null && expected is null) 
            return ComparisonResult.Empty;

        var result = ComparisonResult.Empty;

        if (actual is null || expected is null)
        {
            result += HandleNullElements(actual, expected, currentXPath);
            return result;
        }

        if (actual.Name != expected.Name)
        {
            result += new ComparisonResult.Difference($"Names are different at {currentXPath}", expected.Name, actual.Name);
        }

        result += CompareAttributes(actual, expected, currentXPath, options);
        result += CompareContent(actual, expected, currentXPath);
        result += CompareChildElements(actual, expected, currentXPath, options);

        return result;
    }

    private static ComparisonResult CompareContent(XmlNode actual, XmlNode expected, XPath currentXPath)
    {
        if (!actual.HasChildren && !expected.HasChildren && actual.Value != expected.Value)
        {
            return new ComparisonResult.Difference($"Values are different at {currentXPath}", expected.Value,
                actual.Value);
        }

        return ComparisonResult.Empty;
    }

    private static ComparisonResult HandleNullElements(XmlNode? actual, XmlNode? expected, XPath currentXPath)
    {
        if (actual is null)
        {
            return new ComparisonResult.Difference($"Actual element at {currentXPath} is null");
        }

        if (expected is null)
        {
            return new ComparisonResult.Difference($"Expected element at {currentXPath} is null");
        }

        return ComparisonResult.Empty;
    }

    private static ComparisonResult CompareAttributes(XmlNode actual, XmlNode expected, XPath currentXPath, ComparisonOptions options)
    {
        var result = ComparisonResult.Empty;

        foreach (var actualAttribute in actual.Element.Attributes())
        {
            var attributeName = actualAttribute.Name;
            var attributePath = currentXPath.Append($"@{attributeName}");
            
            if (options.IsExcluded(attributePath))
                continue;
            
            var expectedAttribute = expected.Element.Attribute(attributeName);
            
            if (expectedAttribute is null)
            {
                result += new ComparisonResult.Difference($"Unexpected attribute at {attributePath}");
            }
            else if (actualAttribute.Value != expectedAttribute.Value)
            {
                result += new ComparisonResult.Difference($"Values are different at {attributePath}", expectedAttribute.Value, actualAttribute.Value);
            }
        }

        foreach (var attribute in expected.Element.Attributes())
        {
            if (actual.Element.Attribute(attribute.Name) is null)
            {
                result += new ComparisonResult.Difference($"Missing attribute {attribute.Name.LocalName} at {currentXPath}");
            }
        }

        return result;
    }

    private static ComparisonResult CompareChildElements(XmlNode actual, XmlNode expected, XPath currentXPath, ComparisonOptions options)
    {
        var result = ComparisonResult.Empty;

        var groupedChildren = actual.Children.GroupBy(child => child.Name)
            .ToDictionary(x => x.Key, x => x.ToArray());

        foreach (var group in groupedChildren)
        {
            if (group.Value.Length > 1)
            {
                result += CompareArrays(group.Key, group.Value, expected, currentXPath, options);
            }
            else
            {
                var actualChild = group.Value.Single();
                
                var matchingExpectedChild = expected.Find(actualChild.XPath);
            
                if (matchingExpectedChild is null)
                {
                    result += new ComparisonResult.Difference($"Unexpected element at {actualChild.XPath}");
                    continue;
                }
                result += Compare(actualChild, matchingExpectedChild, actualChild.XPath, options);
            }
        }
        
        var groupedExpectedChildren = expected.Children.GroupBy(child => child.Name)
            .ToDictionary(x => x.Key, x => x.ToArray());

        foreach (var group in groupedExpectedChildren)
        {
            if (group.Value.Length > 1)
            {
                continue; // arrays are handled earlier
            }

            var expectedElement = group.Value.Single();
            result += EnsureNoMissingElements(actual, currentXPath, expectedElement);
        }

        return result;
    }

    private static ComparisonResult EnsureNoMissingElements(XmlNode actual, XPath currentXPath,
        XmlNode expectedChild)
    {
        var matchingActualChild = actual.Find(expectedChild.XPath);

        if (matchingActualChild is null)
        {
            return new ComparisonResult.Difference($"Missing element {expectedChild.Name} at {currentXPath}");
        }

        return ComparisonResult.Empty;
    }

    private static ComparisonResult CompareArrays(string arrayName, XmlNode[] array, XmlNode expected, XPath currentXPath, ComparisonOptions options)
    {
        var result = ComparisonResult.Empty;

        var index = 1; // arrays in xml are 1 based

        foreach (var actualArrayElement in array)
        {
            var arrayElementPath = currentXPath.Append($"{arrayName}[{index}]");

            var expectedArrayElement = expected.Find(arrayElementPath);

            if (expectedArrayElement is null)
            {
                result += new ComparisonResult.Difference($"Unexpected element at {arrayElementPath}");
            }
            else
            {
                result += Compare(actualArrayElement.WithXPath(arrayElementPath), expectedArrayElement, arrayElementPath, options);
            }

            index++;
        }

        var expectedArray = expected.Children.Where(c => c.Name == arrayName).ToArray();

        if (expectedArray.Length > array.Length)
        {
            foreach (var missingElementIndex in Enumerable.Range(array.Length + 1, expectedArray.Length - array.Length))
            {
                var arrayElementPath = currentXPath.Append($"{arrayName}[{missingElementIndex}]");
                result += new ComparisonResult.Difference($"Missing value at {arrayElementPath}");
            }
        }

        return result;
    }
    
        private class XPath : IEquatable<XPath> 
    {
        private readonly string _xPath;
        public static readonly XPath Root = new("/");
        private XPath(string xPath)
        {
            _xPath = xPath;
        }

        public XPath Append(string subXPath)
        {
            if (_xPath.EndsWith("/"))
            {
                if (subXPath.StartsWith("/"))
                {
                    return new XPath($"{_xPath}{subXPath.TrimStart('/')}");    
                }
                
                return new XPath($"{_xPath}{subXPath}");
            }

            if (!subXPath.StartsWith("/"))
            {
                return new XPath($"{_xPath}/{subXPath}");
            }
            
            return new XPath($"{_xPath}{subXPath}");
        }

        public static implicit operator string(XPath xpath) => xpath._xPath;

        public override string ToString()
        {
            return _xPath;
        }

        public bool Equals(XPath? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return _xPath == other._xPath;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((XPath) obj);
        }

        public override int GetHashCode()
        {
            return _xPath.GetHashCode();
        }

        public static bool operator ==(XPath? left, XPath? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(XPath? left, XPath? right)
        {
            return !Equals(left, right);
        }
    }
    private class XmlNode
    {
        
        private readonly XElement _root;

        private XmlNode(XPath xPath, XElement element, XElement root)
        {
            XPath = xPath;
            Element = element;
            _root = root;
        }

        public XPath XPath { get; init; }
        public XElement Element { get; }

        public IEnumerable<XmlNode> Children => Element.Elements().Select(element => new XmlNode(XPath.Append(element.Name.LocalName), element,_root));
        
        public string Name => Element.Name.LocalName;
        
        public bool HasChildren => Element.HasElements;
        public string Value => Element.Value;

        public static XmlNode? FromRoot(XElement? element)
        {
            if (element is null)
            {
                return null;
            }
            
            return new XmlNode(XPath.Root, element,element);
        }

        public XmlNode? Find(XPath path)
        {
            if (path == XPath)
            {
                return this;
            }
            
            var element = _root.XPathSelectElement(path);

            if (element is null)
            {
                return null;
            }

            return new XmlNode(path, element, _root);
        }

        public XmlNode WithXPath(XPath xPath)
        {
            return new XmlNode(xPath, Element, _root);
        }

        public override string ToString()
        {
            return Element.ToString();
        }
    } 
}
