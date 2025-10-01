using LankaConnect.Domain.Common;
using LankaConnect.Domain.Shared.ValueObjects;

namespace LankaConnect.Domain.Enterprise.ValueObjects;

/// <summary>
/// Total contract value for enterprise agreements
/// Represents annual recurring revenue with enterprise pricing tiers
/// </summary>
public class ContractValue : ValueObject
{
    public Money AnnualValue { get; }
    public Money MonthlyValue { get; }
    public Money SetupFee { get; }
    public ContractTier Tier { get; }
    public bool IncludesConsulting { get; }
    public bool IncludesCustomDevelopment { get; }
    public int ContractTermMonths { get; }

    public ContractValue(
        Money annualValue,
        Money setupFee,
        ContractTier tier,
        bool includesConsulting = false,
        bool includesCustomDevelopment = false,
        int contractTermMonths = 12)
    {
        if (annualValue.Amount <= 0)
            throw new ArgumentException("Annual contract value must be positive.", nameof(annualValue));
            
        if (contractTermMonths <= 0)
            throw new ArgumentException("Contract term must be positive.", nameof(contractTermMonths));
            
        if (!annualValue.Currency.Equals(setupFee.Currency))
            throw new ArgumentException("Annual value and setup fee must use the same currency.");

        AnnualValue = annualValue;
        MonthlyValue = new Money(annualValue.Amount / 12, annualValue.Currency);
        SetupFee = setupFee;
        Tier = tier;
        IncludesConsulting = includesConsulting;
        IncludesCustomDevelopment = includesCustomDevelopment;
        ContractTermMonths = contractTermMonths;
    }

    /// <summary>
    /// Creates Fortune 500 enterprise contract ($500K+ annual value)
    /// </summary>
    public static ContractValue CreateFortune500Contract(Money annualValue, Money? setupFee = null)
    {
        if (annualValue.Amount < 500000)
            throw new ArgumentException("Fortune 500 contracts must have minimum $500K annual value.");
            
        return new ContractValue(
            annualValue,
            setupFee ?? new Money(50000, annualValue.Currency),
            ContractTier.Fortune500,
            includesConsulting: true,
            includesCustomDevelopment: true,
            contractTermMonths: 36); // 3-year commitment
    }

    /// <summary>
    /// Creates mid-market enterprise contract ($50K-$500K annual value)
    /// </summary>
    public static ContractValue CreateMidMarketContract(Money annualValue, Money? setupFee = null)
    {
        if (annualValue.Amount < 50000 || annualValue.Amount >= 500000)
            throw new ArgumentException("Mid-market contracts must be between $50K-$500K annual value.");
            
        return new ContractValue(
            annualValue,
            setupFee ?? new Money(10000, annualValue.Currency),
            ContractTier.MidMarket,
            includesConsulting: true,
            includesCustomDevelopment: false,
            contractTermMonths: 24); // 2-year commitment
    }

    /// <summary>
    /// Creates educational institution contract (special pricing)
    /// </summary>
    public static ContractValue CreateEducationalContract(Money annualValue, Money? setupFee = null)
    {
        return new ContractValue(
            annualValue,
            setupFee ?? new Money(5000, annualValue.Currency),
            ContractTier.Educational,
            includesConsulting: true,
            includesCustomDevelopment: false,
            contractTermMonths: 12);
    }

    /// <summary>
    /// Creates government agency contract (compliance-focused pricing)
    /// </summary>
    public static ContractValue CreateGovernmentContract(Money annualValue, Money? setupFee = null)
    {
        return new ContractValue(
            annualValue,
            setupFee ?? new Money(15000, annualValue.Currency),
            ContractTier.Government,
            includesConsulting: true,
            includesCustomDevelopment: false,
            contractTermMonths: 60); // 5-year commitment typical for government
    }

    public Money TotalContractValue => new(AnnualValue.Amount * (ContractTermMonths / 12.0m) + SetupFee.Amount, AnnualValue.Currency);
    public bool IsEnterpriseTier => AnnualValue.Amount >= 500000;
    public bool IsMidMarketTier => AnnualValue.Amount >= 50000 && AnnualValue.Amount < 500000;
    public bool RequiresApproval => AnnualValue.Amount >= 100000;
    public bool QualifiesForVolumeDiscount => ContractTermMonths >= 24;

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return AnnualValue;
        yield return SetupFee;
        yield return Tier;
        yield return IncludesConsulting;
        yield return IncludesCustomDevelopment;
        yield return ContractTermMonths;
    }
}

public enum ContractTier
{
    Standard,
    MidMarket,
    Fortune500,
    Educational,
    Government,
    NonProfit
}