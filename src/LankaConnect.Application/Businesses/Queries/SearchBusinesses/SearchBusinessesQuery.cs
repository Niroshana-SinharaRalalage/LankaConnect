using LankaConnect.Application.Businesses.Common;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Common.Models;

namespace LankaConnect.Application.Businesses.Queries.SearchBusinesses;

public record SearchBusinessesQuery(
    string? SearchTerm,
    string? Category,
    string? City,
    string? Province,
    double? Latitude,
    double? Longitude,
    double? RadiusKm,
    decimal? MinRating,
    bool? IsVerified,
    int PageNumber,
    int PageSize
) : IQuery<PaginatedList<BusinessDto>>;