using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Enterprise.ValueObjects;

public class AnalyticsRequest : ValueObject
{
    public EnterpriseClientId ClientId { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public IReadOnlyList<string> MetricTypes { get; private set; }
    public IReadOnlyList<string> Dimensions { get; private set; }
    public string? FilterExpression { get; private set; }
    public int MaxResults { get; private set; }
    public string? SortBy { get; private set; }
    public bool IncludeCulturalIntelligence { get; private set; }

    private AnalyticsRequest(
        EnterpriseClientId clientId,
        DateTime startDate,
        DateTime endDate,
        IReadOnlyList<string> metricTypes,
        IReadOnlyList<string> dimensions,
        string? filterExpression = null,
        int maxResults = 1000,
        string? sortBy = null,
        bool includeCulturalIntelligence = false)
    {
        ClientId = clientId;
        StartDate = startDate;
        EndDate = endDate;
        MetricTypes = metricTypes;
        Dimensions = dimensions;
        FilterExpression = filterExpression;
        MaxResults = maxResults;
        SortBy = sortBy;
        IncludeCulturalIntelligence = includeCulturalIntelligence;
    }

    public static AnalyticsRequest Create(
        EnterpriseClientId clientId,
        DateTime startDate,
        DateTime endDate,
        IEnumerable<string> metricTypes,
        IEnumerable<string> dimensions,
        string? filterExpression = null,
        int maxResults = 1000,
        string? sortBy = null,
        bool includeCulturalIntelligence = false)
    {
        if (clientId == null) throw new ArgumentNullException(nameof(clientId));
        if (startDate >= endDate) throw new ArgumentException("Start date must be before end date", nameof(startDate));
        if (maxResults <= 0 || maxResults > 10000) throw new ArgumentException("Max results must be between 1 and 10000", nameof(maxResults));

        var metricTypesList = metricTypes?.ToList() ?? throw new ArgumentNullException(nameof(metricTypes));
        var dimensionsList = dimensions?.ToList() ?? throw new ArgumentNullException(nameof(dimensions));

        if (!metricTypesList.Any()) throw new ArgumentException("At least one metric type is required", nameof(metricTypes));
        if (!dimensionsList.Any()) throw new ArgumentException("At least one dimension is required", nameof(dimensions));

        return new AnalyticsRequest(
            clientId,
            startDate,
            endDate,
            metricTypesList.AsReadOnly(),
            dimensionsList.AsReadOnly(),
            filterExpression,
            maxResults,
            sortBy,
            includeCulturalIntelligence);
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return ClientId;
        yield return StartDate;
        yield return EndDate;
        foreach (var metricType in MetricTypes)
            yield return metricType;
        foreach (var dimension in Dimensions)
            yield return dimension;
        yield return FilterExpression ?? string.Empty;
        yield return MaxResults;
        yield return SortBy ?? string.Empty;
        yield return IncludeCulturalIntelligence;
    }
}