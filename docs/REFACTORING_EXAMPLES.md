# File Organization Refactoring - Concrete Examples

This document provides specific, actionable examples of how to refactor the identified violations.

---

## Example 1: Controller with Inline DTOs

### Current State (VIOLATION)

**File**: `src\LankaConnect.API\Controllers\BusinessesController.cs`

```csharp
// Lines 1-393: Controller implementation
public class BusinessesController : ControllerBase
{
    // Controller methods...
}

// Lines 394-420: Inline DTO definitions
public record CreateBusinessResponse(Guid BusinessId);

public record UpdateBusinessRequest(
    string Name,
    string Description,
    // ... other properties
);

public record AddServiceRequest(
    Guid BusinessId,
    string ServiceName,
    decimal Price
);

public record AddServiceResponse(Guid ServiceId);

public record ReorderImagesRequest(List<string> ImageIds);
```

### Refactored Structure (CORRECT)

**Step 1**: Create DTO directory
```
src/LankaConnect.API/DTOs/Businesses/
```

**Step 2**: Extract each record to separate file

**File**: `src\LankaConnect.API\DTOs\Businesses\CreateBusinessResponse.cs`
```csharp
namespace LankaConnect.API.DTOs.Businesses;

public record CreateBusinessResponse(Guid BusinessId);
```

**File**: `src\LankaConnect.API\DTOs\Businesses\UpdateBusinessRequest.cs`
```csharp
namespace LankaConnect.API.DTOs.Businesses;

public record UpdateBusinessRequest(
    string Name,
    string Description,
    // ... other properties
);
```

**File**: `src\LankaConnect.API\DTOs\Businesses\AddServiceRequest.cs`
```csharp
namespace LankaConnect.API.DTOs.Businesses;

public record AddServiceRequest(
    Guid BusinessId,
    string ServiceName,
    decimal Price
);
```

**File**: `src\LankaConnect.API\DTOs\Businesses\AddServiceResponse.cs`
```csharp
namespace LankaConnect.API.DTOs.Businesses;

public record AddServiceResponse(Guid ServiceId);
```

**File**: `src\LankaConnect.API\DTOs\Businesses\ReorderImagesRequest.cs`
```csharp
namespace LankaConnect.API.DTOs.Businesses;

public record ReorderImagesRequest(List<string> ImageIds);
```

**Step 3**: Update controller file

**File**: `src\LankaConnect.API\Controllers\BusinessesController.cs`
```csharp
using LankaConnect.Application.Businesses.Commands.AddService;
using LankaConnect.Application.Businesses.Commands.CreateBusiness;
using LankaConnect.Application.Businesses.Commands.DeleteBusiness;
using LankaConnect.Application.Businesses.Commands.UpdateBusiness;
using LankaConnect.Application.Businesses.Commands.UploadBusinessImage;
using LankaConnect.Application.Businesses.Commands.DeleteBusinessImage;
using LankaConnect.Application.Businesses.Commands.ReorderBusinessImages;
using LankaConnect.Application.Businesses.Commands.SetPrimaryBusinessImage;
using LankaConnect.Application.Businesses.Common;
using LankaConnect.Application.Businesses.Queries.GetBusiness;
using LankaConnect.Application.Businesses.Queries.GetBusinessServices;
using LankaConnect.Application.Businesses.Queries.GetBusinessImages;
using LankaConnect.Application.Businesses.Queries.SearchBusinesses;
using LankaConnect.Application.Common.Models;
using LankaConnect.API.DTOs.Businesses; // ADD THIS
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LankaConnect.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class BusinessesController : ControllerBase
{
    // Controller implementation only - DTOs removed
}
```

**Result**:
- Before: 1 file with 6 types
- After: 6 files with 1 type each

---

## Example 2: Interface File with Embedded Implementation and Events

### Current State (VIOLATION)

**File**: `src\LankaConnect.Application\Billing\StripeWebhookHandler.cs`

```csharp
// Line 477: Interface definition
public interface IStripeWebhookHandler
{
    Task HandleWebhookAsync(string payload, string signature);
}

// Line 16: Implementation class
public class StripeWebhookHandler : IStripeWebhookHandler
{
    // Implementation...
}

// Lines 483-594: Data classes and event classes
public class SubscriptionData { }
public class InvoiceData { }
public class UsageRecordData { }
public class PayoutData { }
public class CulturalIntelligenceSubscriptionActivatedEvent { }
public class CulturalIntelligenceSubscriptionUpdatedEvent { }
public class CulturalIntelligenceSubscriptionCancelledEvent { }
public class CulturalIntelligencePaymentSucceededEvent { }
public class CulturalIntelligencePaymentFailedEvent { }
public class CulturalIntelligenceTrialEndingEvent { }
public class PartnershipPayoutCompletedEvent { }
public class PartnershipPayoutFailedEvent { }
public class UnknownStripeWebhookEvent { }
```

### Refactored Structure (CORRECT)

**Directory Structure**:
```
src/LankaConnect.Application/Billing/
├── Interfaces/
│   └── IStripeWebhookHandler.cs          (interface only)
├── Services/
│   └── StripeWebhookHandler.cs            (implementation only)
├── Models/
│   ├── SubscriptionData.cs
│   ├── InvoiceData.cs
│   ├── UsageRecordData.cs
│   └── PayoutData.cs
└── Events/
    ├── SubscriptionActivatedEvent.cs
    ├── SubscriptionUpdatedEvent.cs
    ├── SubscriptionCancelledEvent.cs
    ├── PaymentSucceededEvent.cs
    ├── PaymentFailedEvent.cs
    ├── TrialEndingEvent.cs
    ├── PartnershipPayoutCompletedEvent.cs
    ├── PartnershipPayoutFailedEvent.cs
    └── UnknownStripeWebhookEvent.cs
```

**File**: `src\LankaConnect.Application\Billing\Interfaces\IStripeWebhookHandler.cs`
```csharp
namespace LankaConnect.Application.Billing.Interfaces;

public interface IStripeWebhookHandler
{
    Task HandleWebhookAsync(string payload, string signature);
}
```

**File**: `src\LankaConnect.Application\Billing\Services\StripeWebhookHandler.cs`
```csharp
using LankaConnect.Application.Billing.Interfaces;
using LankaConnect.Application.Billing.Models;
using LankaConnect.Application.Billing.Events;

namespace LankaConnect.Application.Billing.Services;

public class StripeWebhookHandler : IStripeWebhookHandler
{
    // Implementation only - no embedded types
}
```

**File**: `src\LankaConnect.Application\Billing\Models\SubscriptionData.cs`
```csharp
namespace LankaConnect.Application.Billing.Models;

public class SubscriptionData
{
    public string SubscriptionId { get; set; }
    public string CustomerId { get; set; }
    // ... properties
}
```

**File**: `src\LankaConnect.Application\Billing\Events\SubscriptionActivatedEvent.cs`
```csharp
namespace LankaConnect.Application.Billing.Events;

public class CulturalIntelligenceSubscriptionActivatedEvent
{
    public string SubscriptionId { get; set; }
    public DateTime ActivatedAt { get; set; }
    // ... properties
}
```

**Result**:
- Before: 1 file with 15 types
- After: 15 files with 1 type each, organized by responsibility

---

## Example 3: Large DTO Collection File

### Current State (VIOLATION)

**File**: `src\LankaConnect.API\DTOs\CulturalIntelligenceBillingDTOs.cs`

Contains 28 types:
- Subscription requests/responses (8 types)
- Usage requests/responses (10 types)
- Enterprise requests/responses (4 types)
- Analytics requests/responses (6 types)

### Refactored Structure (CORRECT)

**Directory Structure**:
```
src/LankaConnect.API/DTOs/CulturalIntelligenceBilling/
├── Subscriptions/
│   ├── CreateCulturalIntelligenceSubscriptionRequest.cs
│   ├── CulturalIntelligenceTierResponse.cs
│   ├── SubscriptionActivatedEvent.cs
│   ├── SubscriptionUpdatedEvent.cs
│   ├── SubscriptionCancelledEvent.cs
│   ├── PaymentSucceededEvent.cs
│   ├── PaymentFailedEvent.cs
│   └── TrialEndingEvent.cs
├── Usage/
│   ├── ProcessCulturalIntelligenceUsageRequest.cs
│   ├── ProcessBuddhistCalendarUsageRequest.cs
│   ├── ProcessCulturalAppropriatenessUsageRequest.cs
│   ├── ProcessDiasporaAnalyticsUsageRequest.cs
│   ├── UsageProcessingResponse.cs
│   ├── BuddhistCalendarUsageResponse.cs
│   ├── CulturalAppropriatenessUsageResponse.cs
│   ├── DiasporaAnalyticsUsageResponse.cs
│   ├── UsagePricingResponse.cs
│   └── SLAResponse.cs
├── Enterprise/
│   ├── CreateEnterpriseContractRequest.cs
│   ├── EnterpriseContractResponse.cs
│   ├── CulturalServiceRequest.cs
│   └── WhiteLabelLicensingRequest.cs
├── Analytics/
│   ├── GetRevenueAnalyticsRequest.cs
│   ├── RevenueAnalyticsResponse.cs
│   ├── UsageAnalyticsResponse.cs
│   ├── CustomerAnalyticsResponse.cs
│   └── CulturalAnalyticsResponse.cs
└── Common/
    ├── PriceResponse.cs
    ├── RequestLimitResponse.cs
    ├── CulturalFeaturesResponse.cs
    ├── BillingErrorResponse.cs
    └── BillingSuccessResponse.cs
```

**Example File**: `src\LankaConnect.API\DTOs\CulturalIntelligenceBilling\Subscriptions\CreateCulturalIntelligenceSubscriptionRequest.cs`
```csharp
namespace LankaConnect.API.DTOs.CulturalIntelligenceBilling.Subscriptions;

public class CreateCulturalIntelligenceSubscriptionRequest
{
    public string CustomerId { get; set; }
    public string TierId { get; set; }
    public string BillingPeriod { get; set; }
    // ... properties
}
```

**Result**:
- Before: 1 massive file with 28 types (very hard to navigate)
- After: 28 files organized by feature area (easy to find and maintain)

---

## Example 4: Disaster Recovery Configuration with Mixed Types

### Current State (VIOLATION)

**File**: `src\LankaConnect.Application\Common\DisasterRecovery\AlternativeChannelConfiguration.cs`

Contains 15 types:
- 7 classes: AlternativeChannelConfiguration, AlternativeRevenueChannel, AlternativeChannelCapacity, ChannelFailoverRule, AlternativeChannelConstraint, CulturalChannelConfiguration, DiasporaChannelMapping
- 8 enums: AlternativeChannelType, AlternativeChannelScope, AlternativeChannelPriority, AlternativeChannelCapacityStatus, ChannelFailoverTrigger, ChannelFailoverStrategy, AlternativeChannelConstraintType, AlternativeChannelConstraintSeverity

### Refactored Structure (CORRECT)

**Directory Structure**:
```
src/LankaConnect.Application/Common/DisasterRecovery/AlternativeChannel/
├── Configuration/
│   ├── AlternativeChannelConfiguration.cs          (main)
│   ├── CulturalChannelConfiguration.cs
│   └── DiasporaChannelMapping.cs
├── Models/
│   ├── AlternativeRevenueChannel.cs
│   ├── AlternativeChannelCapacity.cs
│   ├── ChannelFailoverRule.cs
│   └── AlternativeChannelConstraint.cs
└── Enums/
    ├── AlternativeChannelType.cs
    ├── AlternativeChannelScope.cs
    ├── AlternativeChannelPriority.cs
    ├── AlternativeChannelCapacityStatus.cs
    ├── ChannelFailoverTrigger.cs
    ├── ChannelFailoverStrategy.cs
    ├── AlternativeChannelConstraintType.cs
    └── AlternativeChannelConstraintSeverity.cs
```

**File**: `src\LankaConnect.Application\Common\DisasterRecovery\AlternativeChannel\Configuration\AlternativeChannelConfiguration.cs`
```csharp
using LankaConnect.Application.Common.DisasterRecovery.AlternativeChannel.Models;
using LankaConnect.Application.Common.DisasterRecovery.AlternativeChannel.Enums;

namespace LankaConnect.Application.Common.DisasterRecovery.AlternativeChannel.Configuration;

public class AlternativeChannelConfiguration
{
    public AlternativeChannelType Type { get; set; }
    public AlternativeChannelScope Scope { get; set; }
    public AlternativeChannelPriority Priority { get; set; }
    public List<AlternativeRevenueChannel> Channels { get; set; }
    public List<ChannelFailoverRule> FailoverRules { get; set; }
    // ... properties
}
```

**File**: `src\LankaConnect.Application\Common\DisasterRecovery\AlternativeChannel\Enums\AlternativeChannelType.cs`
```csharp
namespace LankaConnect.Application.Common.DisasterRecovery.AlternativeChannel.Enums;

public enum AlternativeChannelType
{
    DirectSales,
    PartnerNetwork,
    Marketplace,
    Affiliate,
    WhiteLabel,
    Reseller
}
```

**Result**:
- Before: 1 file with 15 types (7 classes + 8 enums mixed together)
- After: 15 files organized by type (Configuration/Models/Enums) for clarity

---

## Example 5: Interface with Embedded Records and Enums

### Current State (VIOLATION)

**File**: `src\LankaConnect.Application\Common\Interfaces\IDatabaseSecurityOptimizationEngine.cs`

Contains 30 types:
- 1 interface: IDatabaseSecurityOptimizationEngine
- 24 records: CulturalContext, CulturalProfile, SecurityProfile, SecurityOptimizationResult, etc.
- 5 enums: SensitivityLevel, CulturalSignificance, SacredSignificanceLevel, ComplianceLevel, AccessLevel

### Refactored Structure (CORRECT)

**Directory Structure**:
```
src/LankaConnect.Application/Common/DatabaseSecurity/
├── Interfaces/
│   └── IDatabaseSecurityOptimizationEngine.cs     (interface only)
├── Models/
│   ├── CulturalContext.cs
│   ├── CulturalProfile.cs
│   ├── CulturalEventInstance.cs
│   ├── SacredEvent.cs
│   ├── SecurityProfile.cs
│   ├── SecurityOptimizationResult.cs
│   ├── CulturalSecurityResult.cs
│   ├── ComplianceFramework.cs
│   ├── ComplianceValidationResult.cs
│   ├── SOC2ComplianceResult.cs
│   ├── SecurityIncident.cs
│   ├── IncidentResponseResult.cs
│   ├── CulturalUserProfile.cs
│   ├── CulturalRBACResult.cs
│   ├── MultiRegionSecurityResult.cs
│   ├── CulturalDataSet.cs
│   ├── CulturalDataPrivacyResult.cs
│   ├── SecurityMonitoringIntegrationResult.cs
│   └── SecurityIntegrationReport.cs
└── Enums/
    ├── SensitivityLevel.cs
    ├── CulturalSignificance.cs
    ├── SacredSignificanceLevel.cs
    ├── ComplianceLevel.cs
    └── AccessLevel.cs
```

**File**: `src\LankaConnect.Application\Common\DatabaseSecurity\Interfaces\IDatabaseSecurityOptimizationEngine.cs`
```csharp
using LankaConnect.Application.Common.DatabaseSecurity.Models;

namespace LankaConnect.Application.Common.DatabaseSecurity.Interfaces;

public interface IDatabaseSecurityOptimizationEngine
{
    Task<SecurityOptimizationResult> OptimizeSecurityAsync(CulturalContext context);
    Task<CulturalSecurityResult> ApplyCulturalSecurityAsync(CulturalProfile profile);
    // ... method signatures only - no embedded types
}
```

**File**: `src\LankaConnect.Application\Common\DatabaseSecurity\Models\CulturalContext.cs`
```csharp
using LankaConnect.Application.Common.DatabaseSecurity.Enums;

namespace LankaConnect.Application.Common.DatabaseSecurity.Models;

public record CulturalContext(
    string Community,
    CulturalSignificance Significance,
    SensitivityLevel SensitivityLevel,
    DateTime Timestamp
);
```

**File**: `src\LankaConnect.Application\Common\DatabaseSecurity\Enums\SensitivityLevel.cs`
```csharp
namespace LankaConnect.Application.Common.DatabaseSecurity.Enums;

public enum SensitivityLevel
{
    Public,
    Internal,
    Confidential,
    Restricted,
    Sacred
}
```

**Result**:
- Before: 1 interface file with 30 embedded types
- After: 30 separate files organized by purpose, interface remains pure

---

## Automation Script Template

Use this PowerShell script template to automate the extraction:

```powershell
# Extract-TypeToFile.ps1
param(
    [string]$SourceFile,
    [string]$TypeName,
    [string]$TargetDirectory,
    [string]$Namespace
)

# Read source file
$content = Get-Content $SourceFile -Raw

# Extract type definition (simplified - use Roslyn for production)
$pattern = "(?ms)(public\s+(class|interface|enum|struct|record)\s+$TypeName\b.*?^\})"
$match = [regex]::Match($content, $pattern)

if ($match.Success) {
    $typeDefinition = $match.Value

    # Create target file
    $targetFile = Join-Path $TargetDirectory "$TypeName.cs"

    # Generate file content
    $fileContent = @"
namespace $Namespace;

$typeDefinition
"@

    # Write to file
    Set-Content -Path $targetFile -Value $fileContent

    Write-Host "Extracted $TypeName to $targetFile" -ForegroundColor Green
} else {
    Write-Host "Type $TypeName not found in $SourceFile" -ForegroundColor Red
}
```

**Usage**:
```powershell
.\Extract-TypeToFile.ps1 `
    -SourceFile "C:\Work\LankaConnect\src\LankaConnect.API\Controllers\BusinessesController.cs" `
    -TypeName "CreateBusinessResponse" `
    -TargetDirectory "C:\Work\LankaConnect\src\LankaConnect.API\DTOs\Businesses" `
    -Namespace "LankaConnect.API.DTOs.Businesses"
```

---

## Verification Checklist

After refactoring each file:

- [ ] Source file still compiles
- [ ] New files created in correct directories
- [ ] Namespaces are correct
- [ ] Using statements updated
- [ ] All references updated
- [ ] Tests still pass
- [ ] No duplicate type names
- [ ] Git commit made with clear message

---

## Common Pitfalls to Avoid

1. **Circular Dependencies**: Extracting types may reveal circular dependencies
   - Solution: Use interfaces to break cycles

2. **Namespace Conflicts**: Two types with same name in different contexts
   - Solution: Use more specific namespaces or rename types

3. **Breaking Changes**: Public API surface changes
   - Solution: Use type aliases or facade pattern for backward compatibility

4. **Lost Context**: Extracted types lose contextual meaning
   - Solution: Use descriptive folder names and namespaces

5. **Over-Fragmentation**: Too many tiny files
   - Solution: Group related DTOs in same namespace, but still separate files

---

**Next Steps**: Choose one pattern to start with (recommend Example 1 - Controllers), refactor a single controller as proof-of-concept, then scale to all similar violations.
