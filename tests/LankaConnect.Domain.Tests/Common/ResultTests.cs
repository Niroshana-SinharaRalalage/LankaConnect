using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Tests.Common;

public class ResultTests
{
    [Fact]
    public void Success_Should_CreateSuccessfulResult()
    {
        var result = Result.Success();
        
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Failure_Should_CreateFailedResult_WithSingleError()
    {
        var error = "Test error";
        var result = Result.Failure(error);
        
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Single(result.Errors);
        Assert.Contains(error, result.Errors);
    }

    [Fact]
    public void Failure_Should_CreateFailedResult_WithMultipleErrors()
    {
        var errors = new[] { "Error 1", "Error 2", "Error 3" };
        var result = Result.Failure(errors);
        
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(3, result.Errors.Count());
        Assert.All(errors, error => Assert.Contains(error, result.Errors));
    }

    [Fact]
    public void ImplicitOperator_Should_ConvertFromBoolean_ToSuccessResult()
    {
        Result result = true;
        
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ImplicitOperator_Should_ConvertFromString_ToFailureResult()
    {
        var error = "Test error";
        Result result = error;
        
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Single(result.Errors);
        Assert.Contains(error, result.Errors);
    }

    [Fact]
    public void GenericSuccess_Should_CreateSuccessfulResult_WithValue()
    {
        var value = "test value";
        var result = Result<string>.Success(value);
        
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(value, result.Value);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void GenericFailure_Should_CreateFailedResult_WithoutValue()
    {
        var error = "Test error";
        var result = Result<string>.Failure(error);
        
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Single(result.Errors);
        Assert.Contains(error, result.Errors);
    }

    [Fact]
    public void GenericImplicitOperator_Should_ConvertFromValue_ToSuccessResult()
    {
        var value = "test value";
        Result<string> result = value;
        
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(value, result.Value);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void GenericFailure_Should_ConvertFromString_ToFailureResult()
    {
        var error = "Test error";
        var result = Result<string>.Failure(error);
        
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Single(result.Errors);
        Assert.Contains(error, result.Errors);
    }

    [Fact]
    public void Value_Should_ThrowInvalidOperationException_WhenResultIsFailure()
    {
        var result = Result<string>.Failure("Test error");
        
        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    // Additional comprehensive tests for 100% coverage
    
    [Fact]
    public void Error_Property_Should_ReturnFirstError_WhenMultipleErrorsExist()
    {
        var errors = new[] { "First error", "Second error", "Third error" };
        var result = Result.Failure(errors);
        
        Assert.Equal("First error", result.Error);
    }

    [Fact]
    public void Error_Property_Should_ReturnEmptyString_WhenNoErrorsExist()
    {
        var result = Result.Success();
        
        Assert.Equal(string.Empty, result.Error);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Failure_Should_HandleEdgeCaseErrorMessages(string? errorMessage)
    {
        var result = Result.Failure(errorMessage!);
        
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Single(result.Errors);
        Assert.Contains(errorMessage!, result.Errors);
    }

    [Fact]
    public void Failure_WithNullErrors_Should_UseEmptyArray()
    {
        var result = Result.Failure((IEnumerable<string>?)null!);
        
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Failure_WithEmptyErrorList_Should_CreateFailureResult()
    {
        var result = Result.Failure(Array.Empty<string>());
        
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Empty(result.Errors);
        Assert.Equal(string.Empty, result.Error);
    }

    [Theory]
    [InlineData(false)]
    public void ImplicitOperator_Should_ConvertFromFalse_ToFailureResult(bool success)
    {
        Result result = success;
        
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Single(result.Errors);
        Assert.Contains("Operation failed", result.Errors);
        Assert.Equal("Operation failed", result.Error);
    }

    [Fact]
    public void GenericResult_WithNullValue_Should_AllowNullForReferenceTypes()
    {
        string? nullValue = null;
        var result = Result<string?>.Success(nullValue);
        
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Null(result.Value);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void GenericResult_WithValueType_Should_HandleDefaultValues()
    {
        var result = Result<int>.Success(0);
        
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(0, result.Value);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData(42)]
    [InlineData(-1)]
    [InlineData(int.MaxValue)]
    [InlineData(int.MinValue)]
    public void GenericResult_Should_PreserveValueIntegrity(int testValue)
    {
        var result = Result<int>.Success(testValue);
        
        Assert.True(result.IsSuccess);
        Assert.Equal(testValue, result.Value);
    }

    [Fact]
    public void GenericFailure_WithMultipleErrors_Should_PreserveAllErrors()
    {
        var errors = new[] { "Validation error", "Business rule error", "System error" };
        var result = Result<string>.Failure(errors);
        
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(3, result.Errors.Count());
        Assert.Equal("Validation error", result.Error);
        Assert.All(errors, error => Assert.Contains(error, result.Errors));
    }

    [Fact]
    public void GenericFailure_WithNullErrors_Should_UseEmptyArray()
    {
        var result = Result<string>.Failure((IEnumerable<string>?)null!);
        
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Empty(result.Errors);
        Assert.Equal(string.Empty, result.Error);
    }

    [Fact]
    public void GenericImplicitOperator_WithNullValue_Should_CreateSuccessResult()
    {
        string? nullValue = null;
        Result<string?> result = nullValue;
        
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Null(result.Value);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Result_ErrorsEnumeration_Should_ReflectOriginalList()
    {
        var originalErrors = new List<string> { "Error 1", "Error 2" };
        var result = Result.Failure(originalErrors);
        
        // Initial state
        Assert.Equal(2, result.Errors.Count());
        
        // Modify original list - Result will reflect changes since it stores reference
        originalErrors.Add("Error 3");
        
        // Result will be affected (current behavior)
        Assert.Equal(3, result.Errors.Count());
        Assert.Contains("Error 3", result.Errors);
    }

    [Fact]
    public void GenericResult_ErrorsEnumeration_Should_ReflectOriginalList()
    {
        var originalErrors = new List<string> { "Error 1", "Error 2" };
        var result = Result<string>.Failure(originalErrors);
        
        // Initial state
        Assert.Equal(2, result.Errors.Count());
        
        // Modify original list - Result will reflect changes since it stores reference
        originalErrors.Add("Error 3");
        
        // Result will be affected (current behavior)
        Assert.Equal(3, result.Errors.Count());
        Assert.Contains("Error 3", result.Errors);
    }

    [Fact]
    public void Result_Should_HandleLargeNumberOfErrors()
    {
        var errors = Enumerable.Range(1, 1000).Select(i => $"Error {i}").ToArray();
        var result = Result.Failure(errors);
        
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(1000, result.Errors.Count());
        Assert.Equal("Error 1", result.Error);
    }

    [Theory]
    [InlineData("Special characters: !@#$%^&*()")]
    [InlineData("Unicode: 🚀 ñ ü ☃")]
    [InlineData("Newlines:\nMultiple\nLines")]
    [InlineData("Tabs:\tand\tspaces")]
    public void Result_Should_HandleSpecialCharactersInErrors(string errorMessage)
    {
        var result = Result.Failure(errorMessage);
        
        Assert.False(result.IsSuccess);
        Assert.Contains(errorMessage, result.Errors);
        Assert.Equal(errorMessage, result.Error);
    }

    [Fact]
    public async Task GenericResult_Value_AccessPattern_Should_BeThreadSafe()
    {
        var result = Result<string>.Success("Thread safe value");
        var tasks = new List<Task>();
        var results = new List<string>();
        var lockObject = new object();
        
        // Create multiple tasks accessing the value
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                var value = result.Value;
                lock (lockObject)
                {
                    results.Add(value);
                }
            }));
        }
        
        await Task.WhenAll(tasks.ToArray());
        
        Assert.Equal(10, results.Count);
        Assert.All(results, value => Assert.Equal("Thread safe value", value));
    }
}