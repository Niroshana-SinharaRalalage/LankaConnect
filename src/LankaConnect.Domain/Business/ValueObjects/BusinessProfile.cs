using System.Text.RegularExpressions;
using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Business.ValueObjects;

public class BusinessProfile : ValueObject
{
    public string Name { get; }
    public string Description { get; }
    public string? Website { get; }
    public SocialMediaLinks? SocialMedia { get; }
    public List<string> Services { get; }
    public List<string> Specializations { get; }
    
    // For EF Core
    private BusinessProfile()
    {
        Name = null!;
        Description = null!;
        Website = null;
        SocialMedia = null;
        Services = null!;
        Specializations = null!;
    }
    
    private BusinessProfile(
        string name,
        string description,
        string? website,
        SocialMediaLinks? socialMedia,
        List<string> services,
        List<string> specializations)
    {
        Name = name;
        Description = description;
        Website = website;
        SocialMedia = socialMedia;
        Services = services;
        Specializations = specializations;
    }
    
    private static readonly Regex UrlRegex = new(@"^https?://[^\s/$.?#].[^\s]*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    
    public static Result<BusinessProfile> Create(
        string name,
        string description,
        string? website,
        SocialMediaLinks? socialMedia,
        List<string> services,
        List<string> specializations)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<BusinessProfile>.Failure("Business name is required");
            
        if (string.IsNullOrWhiteSpace(description))
            return Result<BusinessProfile>.Failure("Business description is required");
            
        if (name.Length > 255)
            return Result<BusinessProfile>.Failure("Business name cannot exceed 255 characters");
            
        if (description.Length > 2000)
            return Result<BusinessProfile>.Failure("Business description cannot exceed 2000 characters");
            
        website = website?.Trim();
        if (!string.IsNullOrWhiteSpace(website) && !UrlRegex.IsMatch(website))
            return Result<BusinessProfile>.Failure("Invalid website URL format");
            
        if (services == null || services.Count == 0)
            return Result<BusinessProfile>.Failure("At least one service must be provided");
            
        if (specializations == null || specializations.Count == 0)
            return Result<BusinessProfile>.Failure("At least one specialization must be provided");
            
        return Result<BusinessProfile>.Success(new BusinessProfile(
            name.Trim(),
            description.Trim(),
            website?.Trim(),
            socialMedia,
            services.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).ToList(),
            specializations.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).ToList()
        ));
    }
    
    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Name;
        yield return Description;
        
        if (Website != null)
            yield return Website;
            
        if (SocialMedia != null)
            yield return SocialMedia;
            
        foreach (var service in Services)
            yield return service;
            
        foreach (var specialization in Specializations)
            yield return specialization;
    }
}