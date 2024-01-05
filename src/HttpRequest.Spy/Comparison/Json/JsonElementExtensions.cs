using System.Text.Json.Nodes;
using HttpRequest.Spy.Comparison;
using Json.More;
using Json.Schema;


// ReSharper disable once CheckNamespace
namespace System.Text.Json;

public static class JsonElementExtensions
{
    public static ComparisonResult CompareTo(this JsonElement actual, JsonElement expected,
        Func<ComparisonOptions, ComparisonOptions>? configure = null)
    {
        var options = new ComparisonOptions();
        options = configure?.Invoke(options) ?? options;
            
        var actualNode = actual.AsNode() ?? throw new InvalidJsonElementException();
        var expectedNode = expected.AsNode() ?? throw new InvalidJsonElementException();
            
        return CompareNodes(actualNode, expectedNode, options);
    }
        
    private static ComparisonResult CompareNodes(JsonNode? actual, JsonNode? expected, ComparisonOptions options)
    {
        var actualPath = actual?.GetPath();
        var expectedPath = expected?.GetPath();
            
        if ((actualPath is not null && options.IsExcluded(actualPath)) || (expectedPath is not null && options.IsExcluded(expectedPath)))
        {
            return ComparisonResult.Empty;
        }
            
        if (actual is null && expected is null)
        {
            return ComparisonResult.Empty;
        }

        if (actual is null && expected is not null)
        {
            return new ComparisonResult.Difference($"Missing {expected.GetPath()}: {expected}");
        }
            
        if (actual is not null && expected is null)
        {
            return new ComparisonResult.Difference($"Unexpected {actual.GetPath()}: {actual}");
        }

        var actualSchemaType = actual.GetSchemaValueType();
        var expectedSchemaType = expected.GetSchemaValueType();

        if (actualSchemaType != expectedSchemaType)
        {
            return new ComparisonResult.Difference($"Invalid type at {expectedPath}", $"{expectedSchemaType}", $"{actualSchemaType}");
        }

        return actualSchemaType switch
        {
            SchemaValueType.Object => CompareObjects(actual!.AsObject(), expected!.AsObject(), options),
            SchemaValueType.Array => CompareArrays(actual!.AsArray(), expected!.AsArray(), options),
            SchemaValueType.Boolean => CompareValues<bool>(actual!.AsValue(), expected!.AsValue()),
            SchemaValueType.String => CompareText(actual!.AsValue(), expected!.AsValue()),
            SchemaValueType.Number => CompareValues<decimal>(actual!.AsValue(), expected!.AsValue()),
            SchemaValueType.Integer => CompareValues<int>(actual!.AsValue(), expected!.AsValue()),
            SchemaValueType.Null => ComparisonResult.Empty,
            _ => throw new ArgumentOutOfRangeException(nameof(actual), $"Unexpected Json type : {actualSchemaType}")
        };
    }

    private static ComparisonResult CompareArrays(JsonArray actual, JsonArray expected, ComparisonOptions options)
    {
        var result = ComparisonResult.Empty;

        var matchedActualElements = new List<JsonNode>(); 
            
        for (var index = 0; index < expected.Count; index++)
        {
            var expectedElement = expected[index];

            if (index >= actual.Count)
            {
                result += new ComparisonResult.Difference($"Missing Element {expectedElement!.GetPath()} : {expectedElement}");
                continue;
            }
                
            var samePositionActualElement = actual[index]!;

            if (expectedElement.IsEquivalentTo(samePositionActualElement))
            {
                matchedActualElements.Add(samePositionActualElement);
                continue;
            }

            var actualEquivalentElements =
                actual.Where(element => element.IsEquivalentTo(expectedElement)).ToList();

            if (actualEquivalentElements.Count == 1)
            {
                var actualEquivalentElement = actualEquivalentElements.Single();
                matchedActualElements.Add(actualEquivalentElement!);
                    
                result += new ComparisonResult.Difference(
                    $"Element {expectedElement!.GetPath()} was moved to {actualEquivalentElement!.GetPath()}");
                continue;
            }
                
            if (actualEquivalentElements.Count > 1)
            {
                matchedActualElements.AddRange(actualEquivalentElements!);

                result += new ComparisonResult.Difference(
                    $"Element {expectedElement!.GetPath()} was added {actualEquivalentElements.Count} times :  { string.Join(",", actualEquivalentElements.Select(e => e!.GetPath())) }");
                continue;
            }
                
            matchedActualElements.Add(samePositionActualElement);
                
            result += CompareNodes(samePositionActualElement, expectedElement!, options);
        }

        foreach (var actualElement in actual)
        {
            if (actualElement is null || matchedActualElements.Contains(actualElement))
            {
                continue;
            }

            result += new ComparisonResult.Difference($"Unexpected Element {actualElement.GetPath()} : {actualElement}");
        }
            
        return result;
    }

    private static ComparisonResult CompareObjects(JsonObject actual, JsonObject expected, ComparisonOptions options)
    {
        var result = ComparisonResult.Empty;

        var matchedProperty = new List<string>();
            
        foreach (var (expectedPropertyKey, expectedProperty) in expected)
        {
            if (!actual.TryGetPropertyValue(expectedPropertyKey, out var actualProperty) || actualProperty is null)
            {
                if (expectedProperty is null)
                {
                    continue;
                }
                    
                result += new ComparisonResult.Difference($"Missing property {expectedProperty.GetPath()}");
                continue;
            }
                
            matchedProperty.Add(expectedPropertyKey);
            result += CompareNodes(actualProperty, expectedProperty!, options);
        }
            
            
        foreach (var (actualPropertyKey, unExpectedProperty) in actual)
        {
            if (matchedProperty.Contains(actualPropertyKey))
            {
                continue;
            }
                
            if (!expected.TryGetPropertyValue(actualPropertyKey, out _) && unExpectedProperty is not null)
            {
                result += new ComparisonResult.Difference($"Unexpected property {unExpectedProperty.GetPath()}");
            }
        }
            
        return result;
    }
        
        
    private static ComparisonResult CompareValues<T>(JsonValue actual, JsonValue expected)
    {
        if (!actual.TryGetValue<T>(out var actualValue))
        {
            return new ComparisonResult.Difference($"Could not convert value '{actual}' at {actual.GetPath()} to {typeof(T).Name}");    
        }
            
        if (!expected.TryGetValue<T>(out var expectedValue))
        {
            return new ComparisonResult.Difference($"Could not convert value '{expected}' at {expected.GetPath()} to {typeof(T).Name}");    
        }
            
        if (Equals(actualValue, expectedValue))
        {
            return ComparisonResult.Empty;    
        }

        return new ComparisonResult.Difference($"Values are different at {expected.GetPath()}", WithVisibleBlankChars($"{expectedValue}"), WithVisibleBlankChars($"{actualValue}"));
    }
    
    private static ComparisonResult CompareText(JsonValue actual, JsonValue expected)
    {
        if (!actual.TryGetValue<string>(out var actualValue))
        {
            return new ComparisonResult.Difference($"Could not convert value '{actual}' at {actual.GetPath()} to string");    
        }
            
        if (!expected.TryGetValue<string>(out var expectedValue))
        {
            return new ComparisonResult.Difference($"Could not convert value '{expected}' at {expected.GetPath()} to string");    
        }

        var actualValueExcludingCarriageReturn = actualValue.Replace("\r", "");
        var expectedValueExcludingCarriageReturn = expectedValue.Replace("\r", "");
        
        
        if (Equals(actualValueExcludingCarriageReturn, expectedValueExcludingCarriageReturn))
        {
            return ComparisonResult.Empty;    
        }

        return new ComparisonResult.Difference($"Values are different at {expected.GetPath()}", WithVisibleBlankChars($"{expectedValueExcludingCarriageReturn}"), WithVisibleBlankChars($"{actualValueExcludingCarriageReturn}"));
    }

    private static string WithVisibleBlankChars(string input)
    {
        return input.Replace(" ", "·")
            .Replace("\t", "\\t")
            .Replace("\r", "\\r")
            .Replace("\n", $"\\n{Environment.NewLine}");
    }
}

public class InvalidJsonElementException : Exception
{
}