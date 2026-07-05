namespace LankaConnect.BuildingBlocks.Domain.Exceptions;

public class BusinessNotFoundException : DomainException
{
    public BusinessNotFoundException(Guid businessId) : base($"Business with ID '{businessId}' was not found.")
    {
    }
}