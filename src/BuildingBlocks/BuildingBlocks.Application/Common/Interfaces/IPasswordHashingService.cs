using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.BuildingBlocks.Domain.Models;
using LankaConnect.BuildingBlocks.Domain.ValueObjects;
using LankaConnect.BuildingBlocks.Domain.Enums;
namespace LankaConnect.BuildingBlocks.Application.Common.Interfaces;

public interface IPasswordHashingService
{
    Result<string> HashPassword(string password);
    Result<bool> VerifyPassword(string password, string hashedPassword);
    Result ValidatePasswordStrength(string password);
}