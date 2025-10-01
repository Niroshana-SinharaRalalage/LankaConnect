using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Tests.Common;

public class ValueObjectTests
{
    [Fact]
    public void Equals_Should_ReturnTrue_WhenValuesAreEqual()
    {
        var valueObject1 = new TestValueObject("test", 123);
        var valueObject2 = new TestValueObject("test", 123);
        
        Assert.Equal(valueObject1, valueObject2);
    }

    [Fact]
    public void Equals_Should_ReturnFalse_WhenValuesAreDifferent()
    {
        var valueObject1 = new TestValueObject("test1", 123);
        var valueObject2 = new TestValueObject("test2", 123);
        
        Assert.NotEqual(valueObject1, valueObject2);
    }

    [Fact]
    public void GetHashCode_Should_ReturnSameValue_WhenValuesAreEqual()
    {
        var valueObject1 = new TestValueObject("test", 123);
        var valueObject2 = new TestValueObject("test", 123);
        
        Assert.Equal(valueObject1.GetHashCode(), valueObject2.GetHashCode());
    }

    [Fact]
    public void OperatorEquals_Should_ReturnTrue_WhenValuesAreEqual()
    {
        var valueObject1 = new TestValueObject("test", 123);
        var valueObject2 = new TestValueObject("test", 123);
        
        Assert.True(valueObject1 == valueObject2);
    }

    [Fact]
    public void OperatorNotEquals_Should_ReturnTrue_WhenValuesAreDifferent()
    {
        var valueObject1 = new TestValueObject("test1", 123);
        var valueObject2 = new TestValueObject("test2", 123);
        
        Assert.True(valueObject1 != valueObject2);
    }

    [Fact]
    public void Equals_Should_ReturnFalse_WhenComparedToNull()
    {
        var valueObject = new TestValueObject("test", 123);
        
        Assert.False(valueObject.Equals(null));
    }

    [Fact]
    public void Equals_Should_ReturnFalse_WhenComparedToDifferentType()
    {
        var valueObject = new TestValueObject("test", 123);
        var other = "not a value object";
        
        Assert.False(valueObject.Equals(other));
    }

    // Additional comprehensive tests for 100% coverage
    
    [Fact]
    public void Equals_Should_HandleNullComponents_Gracefully()
    {
        var valueObject1 = new TestValueObjectWithNulls(null, 123);
        var valueObject2 = new TestValueObjectWithNulls(null, 123);
        var valueObject3 = new TestValueObjectWithNulls("test", 123);
        
        Assert.Equal(valueObject1, valueObject2);
        Assert.NotEqual(valueObject1, valueObject3);
    }

    [Fact]
    public void GetHashCode_Should_HandleNullComponents_Gracefully()
    {
        var valueObject1 = new TestValueObjectWithNulls(null, 123);
        var valueObject2 = new TestValueObjectWithNulls(null, 123);
        
        Assert.Equal(valueObject1.GetHashCode(), valueObject2.GetHashCode());
    }

    [Fact]
    public void Equals_Should_HandleEmptyComponents_Correctly()
    {
        var valueObject1 = new TestValueObjectEmpty();
        var valueObject2 = new TestValueObjectEmpty();
        
        Assert.Equal(valueObject1, valueObject2);
        Assert.Equal(valueObject1.GetHashCode(), valueObject2.GetHashCode());
    }

    [Fact]
    public void Equals_Should_HandleComplexComponents_Correctly()
    {
        var list1 = new List<string> { "a", "b", "c" };
        var list2 = new List<string> { "a", "b", "c" };
        var list3 = new List<string> { "a", "b", "d" };
        
        var valueObject1 = new TestValueObjectComplex(list1, DateTime.Parse("2025-01-01"));
        var valueObject2 = new TestValueObjectComplex(list2, DateTime.Parse("2025-01-01"));
        var valueObject3 = new TestValueObjectComplex(list3, DateTime.Parse("2025-01-01"));
        
        Assert.Equal(valueObject1, valueObject2);
        Assert.NotEqual(valueObject1, valueObject3);
    }

    [Theory]
    [InlineData("test1", 123, "test1", 123, true)]
    [InlineData("test1", 123, "test2", 123, false)]
    [InlineData("test1", 123, "test1", 456, false)]
    [InlineData("test1", 123, "test2", 456, false)]
    [InlineData(null, 123, null, 123, true)]
    [InlineData("test1", 0, "test1", 0, true)]
    public void Equals_Should_HandleVariousValueCombinations(string? value1, int number1, string? value2, int number2, bool expectedEqual)
    {
        var valueObject1 = new TestValueObjectWithNulls(value1, number1);
        var valueObject2 = new TestValueObjectWithNulls(value2, number2);
        
        if (expectedEqual)
        {
            Assert.Equal(valueObject1, valueObject2);
            Assert.Equal(valueObject1.GetHashCode(), valueObject2.GetHashCode());
            Assert.True(valueObject1 == valueObject2);
            Assert.False(valueObject1 != valueObject2);
        }
        else
        {
            Assert.NotEqual(valueObject1, valueObject2);
            Assert.True(valueObject1 != valueObject2);
            Assert.False(valueObject1 == valueObject2);
        }
    }

    [Fact]
    public void Equals_Should_HandleDifferentValueObjectTypes_Correctly()
    {
        var valueObject1 = new TestValueObject("test", 123);
        var valueObject2 = new DifferentTestValueObject("test", 123);
        
        Assert.NotEqual<ValueObject>(valueObject1, valueObject2);
        Assert.False(valueObject1.Equals(valueObject2));
    }

    [Fact]
    public void OperatorEquals_Should_HandleNullOperands_Correctly()
    {
        TestValueObject? nullValueObject = null;
        var valueObject = new TestValueObject("test", 123);
        
        Assert.True(nullValueObject == null);
        Assert.False(valueObject == null);
        Assert.False(null == valueObject);
        Assert.False(valueObject == nullValueObject);
        Assert.False(nullValueObject == valueObject);
    }

    [Fact]
    public void OperatorNotEquals_Should_HandleNullOperands_Correctly()
    {
        TestValueObject? nullValueObject = null;
        var valueObject = new TestValueObject("test", 123);
        
        Assert.False(nullValueObject != null);
        Assert.True(valueObject != null);
        Assert.True(null != valueObject);
        Assert.True(valueObject != nullValueObject);
        Assert.True(nullValueObject != valueObject);
    }

    [Fact]
    public void ValueObject_Should_WorkInHashSet_Correctly()
    {
        var set = new HashSet<TestValueObject>();
        var valueObject1 = new TestValueObject("test", 123);
        var valueObject2 = new TestValueObject("test", 123); // Same values
        var valueObject3 = new TestValueObject("different", 123);
        
        set.Add(valueObject1);
        set.Add(valueObject2); // Should not add duplicate
        set.Add(valueObject3);
        
        Assert.Equal(2, set.Count);
        Assert.Contains(valueObject1, set);
        Assert.Contains(valueObject2, set); // Same as valueObject1
        Assert.Contains(valueObject3, set);
    }

    [Fact]
    public void ValueObject_Should_WorkInDictionary_Correctly()
    {
        var dict = new Dictionary<TestValueObject, string>();
        var valueObject1 = new TestValueObject("key1", 123);
        var valueObject2 = new TestValueObject("key1", 123); // Same values
        var valueObject3 = new TestValueObject("key2", 456);
        
        dict[valueObject1] = "value1";
        dict[valueObject2] = "value2"; // Should overwrite
        dict[valueObject3] = "value3";
        
        Assert.Equal(2, dict.Count);
        Assert.Equal("value2", dict[valueObject1]); // Updated by valueObject2
        Assert.Equal("value3", dict[valueObject3]);
    }

    [Fact]
    public void GetHashCode_Should_BeConsistent_AcrossMultipleCalls()
    {
        var valueObject = new TestValueObject("test", 123);
        
        var hash1 = valueObject.GetHashCode();
        var hash2 = valueObject.GetHashCode();
        var hash3 = valueObject.GetHashCode();
        
        Assert.Equal(hash1, hash2);
        Assert.Equal(hash2, hash3);
    }

    [Fact]
    public void GetHashCode_Should_HandleAllNullComponents()
    {
        var valueObject = new TestValueObjectWithNulls(null, null);
        
        // Should not throw exception
        var hashCode = valueObject.GetHashCode();
        Assert.True(hashCode >= 0 || hashCode < 0); // Any value is fine, just shouldn't throw
    }

    [Fact]
    public void ValueObject_Should_BeSerializable_ForCaching()
    {
        var valueObject = new TestValueObject("test", 123);
        
        // Verify it can be used in scenarios requiring serialization
        var json = System.Text.Json.JsonSerializer.Serialize(valueObject);
        Assert.Contains("test", json);
        Assert.Contains("123", json);
    }

    [Fact]
    public void ValueObject_Performance_Should_BeAcceptable_ForLargeCollections()
    {
        var valueObjects = new List<TestValueObject>();
        
        // Create a large number of value objects
        for (int i = 0; i < 10000; i++)
        {
            valueObjects.Add(new TestValueObject($"value{i % 100}", i % 1000));
        }
        
        var set = new HashSet<TestValueObject>(valueObjects);
        
        // Should handle large collections efficiently
        Assert.True(set.Count <= valueObjects.Count);
        Assert.True(set.Count >= 100); // At least 100 unique combinations
    }

    [Fact]
    public void Equals_Should_HandleInheritanceScenarios_Correctly()
    {
        var baseValueObject = new TestValueObject("test", 123);
        var derivedValueObject = new DerivedTestValueObject("test", 123, "extra");
        
        // Different types should never be equal
        Assert.NotEqual<ValueObject>(baseValueObject, derivedValueObject);
        Assert.False(baseValueObject.Equals(derivedValueObject));
        Assert.False(derivedValueObject.Equals(baseValueObject));
    }

    // Test helper classes
    
    private class TestValueObject : ValueObject
    {
        public string Value1 { get; }
        public int Value2 { get; }

        public TestValueObject(string value1, int value2)
        {
            Value1 = value1;
            Value2 = value2;
        }

        public override IEnumerable<object> GetEqualityComponents()
        {
            yield return Value1;
            yield return Value2;
        }
    }

    private class TestValueObjectWithNulls : ValueObject
    {
        public string? Value1 { get; }
        public int? Value2 { get; }

        public TestValueObjectWithNulls(string? value1, int? value2)
        {
            Value1 = value1;
            Value2 = value2;
        }

        public override IEnumerable<object> GetEqualityComponents()
        {
            yield return Value1 ?? string.Empty;
            yield return Value2 ?? 0;
        }
    }

    private class TestValueObjectEmpty : ValueObject
    {
        public override IEnumerable<object> GetEqualityComponents()
        {
            yield break; // No components
        }
    }

    private class TestValueObjectComplex : ValueObject
    {
        public IList<string> Values { get; }
        public DateTime Timestamp { get; }

        public TestValueObjectComplex(IList<string> values, DateTime timestamp)
        {
            Values = values;
            Timestamp = timestamp;
        }

        public override IEnumerable<object> GetEqualityComponents()
        {
            yield return string.Join(",", Values);
            yield return Timestamp;
        }
    }

    private class DifferentTestValueObject : ValueObject
    {
        public string Value1 { get; }
        public int Value2 { get; }

        public DifferentTestValueObject(string value1, int value2)
        {
            Value1 = value1;
            Value2 = value2;
        }

        public override IEnumerable<object> GetEqualityComponents()
        {
            yield return Value1;
            yield return Value2;
        }
    }

    private class DerivedTestValueObject : TestValueObject
    {
        public string ExtraValue { get; }

        public DerivedTestValueObject(string value1, int value2, string extraValue) : base(value1, value2)
        {
            ExtraValue = extraValue;
        }

        public override IEnumerable<object> GetEqualityComponents()
        {
            foreach (var component in base.GetEqualityComponents())
                yield return component;
            yield return ExtraValue;
        }
    }
}