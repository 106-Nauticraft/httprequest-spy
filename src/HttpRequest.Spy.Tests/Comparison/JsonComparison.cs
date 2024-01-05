namespace HttpRequest.Spy.Tests.Comparison;

public class JsonComparison
{
    [Fact]
    public void Compare_two_equivalent_objects()
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

        var comparisonResult = actual.CompareObjectAsJsonTo(expected);

        Check.That(comparisonResult).IsEmpty();
    }
        
    [Fact]
    public void Compare_two_objects_with_different_property_values()
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

        var comparisonResult = actual.CompareObjectAsJsonTo(expected);

        Check.That(comparisonResult).ContainsExactly(
            new ComparisonResult.Difference("Values are different at $.Prop1", "3", "1"),
            new ComparisonResult.Difference("Values are different at $.SubProp.Value", "4", "3")
        );
    }
        
    [Fact]
    public void Compare_two_objects_with_unexpected_property()
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

        var comparisonResult = actual.CompareObjectAsJsonTo(expected);

        Check.That(comparisonResult).ContainsExactly(
            new ComparisonResult.Difference("Unexpected property $.SubProp.Value"),
            new ComparisonResult.Difference("Unexpected property $.Prop2")
        );
    }
        
    [Fact]
    public void Compare_two_objects_with_missing_property()
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

        var comparisonResult = actual.CompareObjectAsJsonTo(expected);

        Check.That(comparisonResult).ContainsExactly(
            new ComparisonResult.Difference("Missing property $.Prop2"),
            new ComparisonResult.Difference("Missing property $.SubProp.Value")
        );
    }
        
    [Fact]
    public void Compare_two_equivalent_arrays()
    {
        var actual = new[] {0, 1, 2, 3};
            
        var expected = new[] {0, 1, 2, 3};

        var comparisonResult = actual.CompareObjectAsJsonTo(expected);

        Check.That(comparisonResult).IsEmpty();
    }
        
    [Fact]
    public void Compare_two_arrays_with_unexpected_value()
    {
        var actual = new[] {0, 1, 2, 3, 5};
            
        var expected = new[] {0, 1, 2, 3};

        var comparisonResult = actual.CompareObjectAsJsonTo(expected);

        Check.That(comparisonResult).ContainsExactly(
            new ComparisonResult.Difference("Unexpected Element $[4] : 5")
        );
    }
        
    [Fact]
    public void Compare_two_arrays_with_misplaced_unexpected_value()
    {
        var actual = new[] {0, 1, 5, 2, 3};
            
        var expected = new[] {0, 1, 2, 3};

        var comparisonResult = actual.CompareObjectAsJsonTo(expected);

        Check.That(comparisonResult).ContainsExactly(
            new ComparisonResult.Difference("Element $[2] was moved to $[3]"),
            new ComparisonResult.Difference("Element $[3] was moved to $[4]"),
            new ComparisonResult.Difference("Unexpected Element $[2] : 5")
        );
    }
        
    [Fact]
    public void Compare_two_arrays_with_missing_value()
    {
        var actual = new[] {0, 1, 2};
            
        var expected = new[] {0, 1, 2, 3};

        var comparisonResult = actual.CompareObjectAsJsonTo(expected);

        Check.That(comparisonResult).ContainsExactly(
            new ComparisonResult.Difference("Missing Element $[3] : 3")
        );
    }
        
    [Fact]
    public void Compare_two_arrays_with_a_different_order()
    {
        var actual = new[] {0, 1, 2, 3};
            
        var expected = new[] {0, 1, 3, 2};

        var comparisonResult = actual.CompareObjectAsJsonTo(expected);

        Check.That(comparisonResult).ContainsExactly(
            new ComparisonResult.Difference("Element $[2] was moved to $[3]"),
            new ComparisonResult.Difference("Element $[3] was moved to $[2]")
        );
    }
    
    [Fact]
    public void Compare_two_text_with_different_newline_chars()
    {
        var actual = "Hello\nHow Are You ?\nRegards,\nYour Mum";
        var expected = "Hello\r\nHow Are You ?\r\nRegards,\r\nYour Mum";
            
        var comparisonResult = actual.CompareObjectAsJsonTo(expected);

        Check.That(comparisonResult).IsEmpty();
    }
    
    [Fact]
    public void Compare_two_different_texts()
    {
        var actual = "Hello";
        var expected = "Goodbye";
            
        var comparisonResult = actual.CompareObjectAsJsonTo(expected);

        Check.That(comparisonResult).ContainsExactly(
            new ComparisonResult.Difference("Values are different at $", "Goodbye", "Hello"));
    }
        
    private class ArrayElement
    {
        public ArrayElement(string prop, int id, decimal? value = null)
        {
            Prop = prop;
            Id = id;
            Value = value;
        }

        public string Prop { get; }
        public int Id { get; }
        public decimal? Value { get; }
    }
        
    [Fact]
    public void Compare_two_objects_with_array_property_with_complex_elements()
    {
        var actual = new {
            Array = new[]
            {
                new ArrayElement("Hello", 1),
                new ArrayElement("Goodbye", 3),
                new ArrayElement("Final Hello", 4),
                new ArrayElement("Hello One More Time", 3),
            }
        };
            
        var expected =  new {
            Array = new[]
            {
                new ArrayElement("Hello", 1),
                new ArrayElement("Hello Again", 2),
                new ArrayElement("Hello One More Time", 3),
                new ArrayElement("Final Hello", 4),
            }
        };

        var comparisonResult = actual.CompareObjectAsJsonTo(expected);

        Check.That(comparisonResult).ContainsExactly(
            new ComparisonResult.Difference("Values are different at $.Array[1].Prop", "Hello·Again", "Goodbye"),
            new ComparisonResult.Difference("Values are different at $.Array[1].Id", "2", "3"),
            new ComparisonResult.Difference("Element $.Array[2] was moved to $.Array[3]"),
            new ComparisonResult.Difference("Element $.Array[3] was moved to $.Array[2]")
        );
    }
        
    [Fact]
    public void Compare_two_objects_excluding_some_path()
    {
        var actual = new
        {
            Prop = new
            {
                Name = "Actual Name"
            },
            Array = new[]
            {
                new ArrayElement("Hello", 1, 1),
                new ArrayElement("Hello", 2, 100),
                new ArrayElement("Hello", 3, 40),
                new ArrayElement("Hello", 4, 213),
            }
        };
            
        var expected = new {
            Prop = new
            {
                Name = "Name"  
            },
            Array = new[]
            {
                new ArrayElement("Hello", 1),
                new ArrayElement("Hello Again", 2),
                new ArrayElement("Hello One More Time", 3),
                new ArrayElement("Final Hello", 4),
            }
        };
        
        var comparisonResult = actual.CompareObjectAsJsonTo(expected, 
            options => options.Exclude("Prop", "Array[*].Prop", "*.Value"));
        
        Check.That(comparisonResult).IsEmpty();
    }
        
    // [Fact]
    // public async Task Compare_a_httpContent_content_to_an_equivalent_object()
    // {
    //     var actual = JsonContent.Create(new
    //     {
    //         Prop = 1,
    //         Prop2 = 2,
    //         SubProp = new {
    //             Array = new [] { "some", "values" }
    //         }
    //     });
    //         
    //     var expected = new
    //     {
    //         prop = 1,
    //         prop2 = 3,
    //         subProp = new {
    //             array = new [] { "some", "other", "value" }
    //         }
    //     };
    //         
    //
    //     var comparisonResult = await actual.CompareContentAsJsonTo(expected);
    //
    //     Check.That(comparisonResult).ContainsExactly(
    //         new ComparisonResult.Difference("Values are different at prop2", "3", "2"),
    //         new ComparisonResult.Difference("Values are different at subProp.array[1]", "other", "values"),
    //         new ComparisonResult.Difference("Missing Element subProp.array[2] : value")
    //     );
    // }
    //
}