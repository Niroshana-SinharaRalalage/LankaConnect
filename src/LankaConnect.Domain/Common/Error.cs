namespace LankaConnect.Domain.Common;

/// <summary>
/// Error type for cultural intelligence and calendar operations
/// Provides structured error reporting for failed operations
/// </summary>
public record Error(string Code, string Message)
{
    public static Error None => new(string.Empty, string.Empty);
    public static Error NullValue => new("Error.NullValue", "Null value was provided");
    public static Error NotFound => new("Error.NotFound", "Resource not found");
    public static Error ValidationFailure => new("Error.ValidationFailure", "Validation failed");
    public static Error UnsupportedOperation => new("Error.UnsupportedOperation", "Operation not supported");
    public static Error CulturalConflict => new("Error.CulturalConflict", "Cultural conflict detected");
    public static Error CalendarError => new("Error.CalendarError", "Calendar operation failed");
    public static Error AppropriatenessError => new("Error.AppropriatenessError", "Cultural appropriateness assessment failed");
    
    public bool IsEmpty => Code == string.Empty;
    public bool HasError => !IsEmpty;
    
    public static Error Create(string code, string message) => new(code, message);
    
    public override string ToString() => $"[{Code}] {Message}";
}