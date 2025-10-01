using System;
using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Common.Models.Performance;

/// <summary>
/// Financial constraints model for revenue optimization
/// TDD Implementation: Defines financial constraints for optimization decisions
/// </summary>
public class FinancialConstraints : BaseEntity
{
    public Guid ConstraintsId { get; set; } = Guid.NewGuid();
    public decimal MaxBudget { get; set; } = 0;
    public decimal MinROI { get; set; } = 0.15m;
    public TimeSpan PaybackPeriod { get; set; } = TimeSpan.FromDays(365);
    public List<BudgetCategory> BudgetCategories { get; set; } = new();
    public Dictionary<string, decimal> RegionalBudgets { get; set; } = new();
    public bool RequireApproval { get; set; } = false;
    public decimal ApprovalThreshold { get; set; } = 10000m;
}

public class BudgetCategory
{
    public string CategoryName { get; set; } = string.Empty;
    public decimal AllocatedAmount { get; set; } = 0;
    public decimal SpentAmount { get; set; } = 0;
    public decimal RemainingAmount => AllocatedAmount - SpentAmount;
}