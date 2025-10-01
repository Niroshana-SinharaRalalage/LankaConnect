namespace LankaConnect.Application.Common.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message ?? string.Empty)
    {
    }

    public NotFoundException(string message, Exception innerException) : base(message ?? string.Empty, innerException)
    {
    }

    public NotFoundException(string name, object key) : base($"Entity \"{name}\" ({key}) was not found.")
    {
    }
}