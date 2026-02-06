using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Tax;

/// <summary>
/// Phase 6A.X: Entity representing US state sales tax rates
/// Stores state-level tax rates for revenue breakdown calculations
/// </summary>
public class StateTaxRate : BaseEntity
{
    /// <summary>
    /// Two-letter state code (e.g., "CA", "NY")
    /// </summary>
    public string StateCode { get; private set; }

    /// <summary>
    /// Full state name (e.g., "California", "New York")
    /// </summary>
    public string StateName { get; private set; }

    /// <summary>
    /// State sales tax rate as decimal (e.g., 0.0725 for 7.25%)
    /// </summary>
    public decimal TaxRate { get; private set; }

    /// <summary>
    /// Date when this tax rate becomes effective
    /// Used for historical tracking when rates change
    /// </summary>
    public DateTime EffectiveDate { get; private set; }

    /// <summary>
    /// Indicates if this is the current active rate for the state
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Source of the tax rate data (e.g., "Tax Foundation 2025", "State Department of Revenue")
    /// </summary>
    public string? DataSource { get; private set; }

    // EF Core constructor
    private StateTaxRate()
    {
        StateCode = null!;
        StateName = null!;
    }

    private StateTaxRate(
        string stateCode,
        string stateName,
        decimal taxRate,
        DateTime effectiveDate,
        bool isActive,
        string? dataSource)
    {
        StateCode = stateCode;
        StateName = stateName;
        TaxRate = taxRate;
        EffectiveDate = effectiveDate;
        IsActive = isActive;
        DataSource = dataSource;
    }

    /// <summary>
    /// Creates a new state tax rate entry
    /// </summary>
    /// <param name="stateCode">Two-letter state code (e.g., "CA")</param>
    /// <param name="stateName">Full state name (e.g., "California")</param>
    /// <param name="taxRate">Tax rate as decimal (e.g., 0.0725 for 7.25%)</param>
    /// <param name="effectiveDate">Date when rate becomes effective</param>
    /// <param name="isActive">Whether this is the current active rate</param>
    /// <param name="dataSource">Source of tax rate data</param>
    /// <returns>Result with StateTaxRate or error</returns>
    public static Result<StateTaxRate> Create(
        string stateCode,
        string stateName,
        decimal taxRate,
        DateTime? effectiveDate = null,
        bool isActive = true,
        string? dataSource = null)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(stateCode))
            return Result<StateTaxRate>.Failure("State code is required");

        if (stateCode.Length != 2)
            return Result<StateTaxRate>.Failure("State code must be exactly 2 characters");

        if (string.IsNullOrWhiteSpace(stateName))
            return Result<StateTaxRate>.Failure("State name is required");

        if (taxRate < 0 || taxRate > 0.5m)
            return Result<StateTaxRate>.Failure("Tax rate must be between 0 and 50%");

        var normalizedStateCode = stateCode.ToUpperInvariant();
        var effective = effectiveDate ?? DateTime.UtcNow;

        return Result<StateTaxRate>.Success(new StateTaxRate(
            normalizedStateCode,
            stateName,
            taxRate,
            effective,
            isActive,
            dataSource
        ));
    }

    /// <summary>
    /// Updates the tax rate (when states change their rates)
    /// </summary>
    public Result UpdateRate(decimal newTaxRate, DateTime effectiveDate, string? dataSource = null)
    {
        if (newTaxRate < 0 || newTaxRate > 0.5m)
            return Result.Failure("Tax rate must be between 0 and 50%");

        TaxRate = newTaxRate;
        EffectiveDate = effectiveDate;
        DataSource = dataSource;
        MarkAsUpdated();

        return Result.Success();
    }

    /// <summary>
    /// Deactivates this tax rate entry (when superseded by a new rate)
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        MarkAsUpdated();
    }

    /// <summary>
    /// Activates this tax rate entry
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        MarkAsUpdated();
    }
}
