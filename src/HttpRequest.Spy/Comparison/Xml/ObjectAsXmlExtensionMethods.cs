using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using HttpRequest.Spy.Comparison;

// ReSharper disable once CheckNamespace
namespace System;

public static class ObjectAsXmlExtensionMethods
{
    public static ComparisonResult CompareObjectAsXmlTo<T>(this T actual, T expected,
        Func<ComparisonOptions, ComparisonOptions>? configure = null)
    {
        var actualXElement = actual?.ToXml();
        var expectedXElement = expected?.ToXml();
       
        return actualXElement
            .CompareTo(expectedXElement, configure);
    }
    
    private static bool IsAnonymousType(Type type)
    {
        var isNotPublicAndSealed = type is { IsSealed: true, IsNotPublic: true };
        var nameContainsAnonymousType = type.Name.Contains("AnonymousType");
        var nameStartsWith = type.Name.StartsWith("<>", StringComparison.OrdinalIgnoreCase);
        var hasNotPublicFlag = type.Attributes.HasFlag(TypeAttributes.NotPublic);
        
        return isNotPublicAndSealed &&
               nameContainsAnonymousType &&
               (nameStartsWith ||
                hasNotPublicFlag);
    }

    public static XElement ToXml(this object input, string? rootElementName = null)
    {
        var type = input.GetType();
      
        if (IsAnonymousType(type))
        {
            return SerializeAnonymousElement(input, rootElementName);
        }

        return SerializeToXElement(input);
    }

    private static readonly Type[] WriteTypes = {
        typeof(string), typeof(DateTime), typeof(Enum), 
        typeof(decimal), typeof(Guid),
    };
    public static bool IsSimpleType(this Type type)
    {
        return type.IsPrimitive || WriteTypes.Contains(type);
    }
    
    private static XElement SerializeAnonymousElement(object input, string? elementName = null)
    {
        var element = XmlConvert.EncodeName(elementName ?? "?anonymous?");
        var ret = new XElement(element);
        
        var type = input.GetType();
        var props = type.GetProperties();

        var elements = props.Select(SerializeProperty)
            .Where(prop => prop is not null)
            .Select(prop => prop!)
            .Cast<object>()
            .ToArray(); 

        ret.Add(elements);
        return ret;

        XElement? SerializeProperty(PropertyInfo prop)
        {
            var name = XmlConvert.EncodeName(prop.Name);
            var val = prop.GetValue(input, null);

            if (prop.PropertyType.IsSimpleType() || val is null)
            {
                return new XElement(name, val);
            }
            
            if (IsAnonymousType(val.GetType()))
            {
                return SerializeAnonymousElement(val, name);
            }

            return new XElement(name, SerializeToXElement(val));
        }
    }
    
    private static XElement SerializeToXElement(object obj)
    {
        using var memoryStream = new MemoryStream();
        using var xmlWriter = XmlWriter.Create(memoryStream);
        var serializer = new XmlSerializer(obj.GetType());
        
        serializer.Serialize(xmlWriter, obj);
        memoryStream.Position = 0;
    
        return XElement.Load(memoryStream);
    }
}