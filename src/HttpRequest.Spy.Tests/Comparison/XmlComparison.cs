using System.Xml.Serialization;
using JetBrains.Annotations;

namespace HttpRequest.Spy.Tests.Comparison;

public class XmlComparison
{
    [Fact]
    public void Compare_two_equivalent_anonymous_objects()
    {
        var actual = new
        {
            Prop1 = 1,
            Prop2 = "2",
            SubProp = new
            {
                Value = 2.3m
            }
        };
            
        var expected = new
        {
            Prop1 = 1,
            Prop2 = "2",
            SubProp = new
            {
                Value = 2.3m
            }
        };

        var comparisonResult = actual.CompareObjectAsXmlTo(expected);

        Check.That(comparisonResult).IsEmpty();
    }
    
    [Fact]
    public void Compare_two_anonymous_objects_with_different_property_values()
    {
        var actual = new
        {
            Prop1 = 1,
            Prop2 = "2",
            SubProp = new
            {
                Value = 3
            }
        };
            
        var expected = new
        {
            Prop1 = 3,
            Prop2 = "2",
            SubProp = new
            {
                Value = 4
            }
        };

        var comparisonResult = actual.CompareObjectAsXmlTo(expected);

        Check.That(comparisonResult).ContainsExactly(
            new ComparisonResult.Difference("Values are different at /Prop1", "3", "1"),
            new ComparisonResult.Difference("Values are different at /SubProp/Value", "4", "3")
        );
    }

    [Fact]
    public void Compare_two_anonymous_objects_with_unexpected_properties()
    {
        var actual = new
        {
            Prop1 = 1,
            Prop2 = "2",
            SubProp = new
            {
                Value = 2.3m
            }
        };
            
        var expected = new
        {
            Prop1 = 1,
            SubProp = new
            {
            }
        };

        var comparisonResult = actual.CompareObjectAsXmlTo<object>(expected);

        Check.That(comparisonResult).ContainsExactly(
            new ComparisonResult.Difference("Unexpected element at /Prop2"),
            new ComparisonResult.Difference("Unexpected element at /SubProp/Value")
        );
    }
    
    [Fact]
    public void Compare_two_anonymous_objects_with_missing_property()
    {
        var actual = new
        {
            Prop1 = 1,
            SubProp = new
            {
            }
        };
            
        var expected = new
        {
            Prop1 = 1,
            Prop2 = "2",
            SubProp = new
            {
                Value = 2.3m
            }
        };

        var comparisonResult = actual.CompareObjectAsXmlTo<object>(expected);

        Check.That(comparisonResult).ContainsExactly(
            new ComparisonResult.Difference("Missing element Value at /SubProp"),
            new ComparisonResult.Difference("Missing element Prop2 at /")
            
        );
    }

    [Fact]
    public void Compare_two_equivalent_arrays()
    {
        var actual = new[] {0, 1, 2, 3};
            
        var expected = new[] {0, 1, 2, 3};

        var comparisonResult = actual.CompareObjectAsXmlTo(expected);

        Check.That(comparisonResult).IsEmpty();
    }
    
    [Fact]
    public void Compare_two_different_arrays()
    {
        var actual = new[] {0, 1, 4, 5};
        
        var expected = new[] {0, 1, 2, 3, 4};

        var comparisonResult = actual.CompareObjectAsXmlTo(expected);

        Check.That(comparisonResult).ContainsExactly(
            new ComparisonResult.Difference("Values are different at /int[3]", "2", "4"),
            new ComparisonResult.Difference("Values are different at /int[4]", "3", "5"),
            new ComparisonResult.Difference("Missing value at /int[5]")
        );
    }


    public record XmlSerializableItem([UsedImplicitly] string Label)
    {
        [UsedImplicitly]
        public XmlSerializableItem() : this("")
        {
        }
    }
    public record XmlSerializableObject(string Text, int Count, [property:XmlAttribute("flag")]bool Flag, XmlSerializableItem[] Items)
    {
        public XmlSerializableObject() : this("", -1, false, Array.Empty<XmlSerializableItem>()) {}
    }

    private static readonly XmlSerializableObject Default =
        new("Lorem Ipsum", 32, true, new [] { new XmlSerializableItem("Item1" ), new XmlSerializableItem("Item2" )});
    
    [Fact]
    public void Compare_two_equivalent_serializable_objects()
    {
        var actual = Default;
        var expected = Default;

        var comparisonResult = actual.CompareObjectAsXmlTo(expected);

        Check.That(comparisonResult).IsEmpty();
    }
    
    [Fact]
    public void Compare_two_different_serializable_objects()
    {
        var actual = Default;
        var expected = Default with
        {
            Flag = false,
            Text = "Dolor",
            Items = new [] { new XmlSerializableItem("Item1.5" ), new XmlSerializableItem("Item2" ) }
        };

        var comparisonResult = actual.CompareObjectAsXmlTo(expected);

        Check.That(comparisonResult).ContainsExactly(
            new ComparisonResult.Difference("Values are different at /@flag", "false", "true"),
            new ComparisonResult.Difference("Values are different at /Text", "Dolor", "Lorem Ipsum"),
            new ComparisonResult.Difference("Values are different at /Items/XmlSerializableItem[1]/Label", "Item1.5", "Item1")
        );
    }
    
    [Fact]
    public void Compare_two_different_serializable_objects_excluding_some_path()
    {
        var actual = Default with
        {
            Flag = false,
            Text = "Alea jacta est"
        };
        var expected = Default;

        var comparisonResult = actual.CompareObjectAsXmlTo(expected, options => options.Exclude("@flag", "/Text"));

        Check.That(comparisonResult).IsEmpty();
    }
    
}