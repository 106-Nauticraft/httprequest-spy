using System.Text.Json;
using HttpRequestSpy.Comparison;

// ReSharper disable once CheckNamespace
namespace System;

public static class ObjectAsJsonExtensionMethods
{
    public static ComparisonResult CompareObjectAsJsonTo(this object actual, object expected,
        Func<ComparisonOptions, ComparisonOptions>? configure = null)
    {
        var actualJsonElement = JsonSerializer.SerializeToElement(actual);
        var expectedJsonElement = JsonSerializer.SerializeToElement(expected);

        return actualJsonElement
            .CompareTo(expectedJsonElement, configure);
    }
}