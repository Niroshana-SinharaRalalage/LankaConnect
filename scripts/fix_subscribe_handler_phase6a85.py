#!/usr/bin/env python3
"""
Phase 6A.85 Part 3: Fix SubscribeToNewsletterCommandHandler
Apply same metro area population logic as CreateNewsletterCommandHandler
"""

import re

file_path = "src/LankaConnect.Application/Communications/Commands/SubscribeToNewsletter/SubscribeToNewsletterCommandHandler.cs"

with open(file_path, 'r', encoding='utf-8') as f:
    content = f.read()

# Step 1: Add using statements
if "using LankaConnect.Domain.Events;" not in content:
    content = content.replace(
        "using LankaConnect.Domain.Communications.Entities;",
        "using LankaConnect.Domain.Communications.Entities;\nusing LankaConnect.Domain.Events;"
    )

if "using Microsoft.EntityFrameworkCore;" not in content:
    content = content.replace(
        "using Serilog.Context;",
        "using Microsoft.EntityFrameworkCore;\nusing Serilog.Context;"
    )

# Step 2: Add IApplicationDbContext field
content = content.replace(
    "    private readonly IEmailService _emailService;\n    private readonly ILogger<SubscribeToNewsletterCommandHandler> _logger;",
    "    private readonly IEmailService _emailService;\n    private readonly IApplicationDbContext _dbContext;\n    private readonly ILogger<SubscribeToNewsletterCommandHandler> _logger;"
)

# Step 3: Add IApplicationDbContext parameter to constructor
content = content.replace(
    "        IEmailService emailService,\n        ILogger<SubscribeToNewsletterCommandHandler> logger,",
    "        IEmailService emailService,\n        IApplicationDbContext dbContext,\n        ILogger<SubscribeToNewsletterCommandHandler> logger,"
)

# Step 4: Assign _dbContext in constructor
content = content.replace(
    "        _emailService = emailService;\n        _logger = logger;",
    "        _emailService = emailService;\n        _dbContext = dbContext;\n        _logger = logger;"
)

# Step 5: Add helper method to populate metros (insert before the Handle method)
helper_method = '''    /// <summary>
    /// Phase 6A.85 Part 3: Populate all metro areas when ReceiveAllLocations = true
    /// Same logic as CreateNewsletterCommandHandler to maintain architectural consistency
    /// </summary>
    private async Task<IEnumerable<Guid>> PopulateMetroAreasIfNeededAsync(
        List<Guid>? requestedMetroAreaIds,
        bool receiveAllLocations,
        CancellationToken cancellationToken)
    {
        // If user selected specific metros, use them as-is
        if (requestedMetroAreaIds != null && requestedMetroAreaIds.Any())
        {
            _logger.LogInformation(
                "[Phase 6A.85 Part 3] Subscriber selected {Count} specific metro area(s)",
                requestedMetroAreaIds.Count);
            return requestedMetroAreaIds;
        }

        // If ReceiveAllLocations = true, query all active metros from database
        if (receiveAllLocations)
        {
            _logger.LogInformation(
                "[Phase 6A.85 Part 3] Subscriber selected 'All Locations' - querying all active metro areas");

            try
            {
                var dbContext = _dbContext as DbContext
                    ?? throw new InvalidOperationException("DbContext must be EF Core DbContext");

                var allMetroAreaIds = await dbContext.Set<MetroArea>()
                    .Where(m => m.IsActive)
                    .Select(m => m.Id)
                    .ToListAsync(cancellationToken);

                _logger.LogInformation(
                    "[Phase 6A.85 Part 3] Successfully populated {Count} metro areas for 'All Locations' subscriber",
                    allMetroAreaIds.Count);

                return allMetroAreaIds;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[Phase 6A.85 Part 3] FAILED to query metro areas for 'All Locations' subscriber");
                throw;
            }
        }

        // Fallback: no metros selected (shouldn't happen due to validation)
        _logger.LogWarning(
            "[Phase 6A.85 Part 3] No metro areas specified and receiveAllLocations=false. Returning empty list.");
        return new List<Guid>();
    }

'''

# Insert helper method before Handle method
content = content.replace(
    "    public async Task<Result<SubscribeToNewsletterResponse>> Handle(",
    helper_method + "    public async Task<Result<SubscribeToNewsletterResponse>> Handle("
)

# Step 6: Fix line 113-120 (reactivate path)
old_reactivate = '''                    // For inactive subscribers, create a new subscription instead of reactivating
                    // This ensures a fresh confirmation token and follows the domain model
                    // Phase 6A.64: Convert single metro area ID to collection for new API
                    var metroAreaIds = request.MetroAreaIds ?? (metroAreaId.HasValue ? new List<Guid> { metroAreaId.Value } : new List<Guid>());

                    var reactivateResult = NewsletterSubscriber.Create(
                        email,
                        metroAreaIds,
                        request.ReceiveAllLocations);'''

new_reactivate = '''                    // For inactive subscribers, create a new subscription instead of reactivating
                    // This ensures a fresh confirmation token and follows the domain model
                    // Phase 6A.85 Part 3: Populate all metros when ReceiveAllLocations = true
                    var metroAreaIds = await PopulateMetroAreasIfNeededAsync(
                        request.MetroAreaIds,
                        request.ReceiveAllLocations,
                        cancellationToken);

                    var reactivateResult = NewsletterSubscriber.Create(
                        email,
                        metroAreaIds.ToList(),
                        request.ReceiveAllLocations);'''

content = content.replace(old_reactivate, new_reactivate)

# Step 7: Fix line 152-158 (new subscriber path)
old_create = '''                    // Phase 6A.64: Convert single metro area ID to collection for new API
                    var metroAreaIds = request.MetroAreaIds ?? (metroAreaId.HasValue ? new List<Guid> { metroAreaId.Value } : new List<Guid>());

                    var createResult = NewsletterSubscriber.Create(
                        email,
                        metroAreaIds,
                        request.ReceiveAllLocations);'''

new_create = '''                    // Phase 6A.85 Part 3: Populate all metros when ReceiveAllLocations = true
                    var metroAreaIds = await PopulateMetroAreasIfNeededAsync(
                        request.MetroAreaIds,
                        request.ReceiveAllLocations,
                        cancellationToken);

                    var createResult = NewsletterSubscriber.Create(
                        email,
                        metroAreaIds.ToList(),
                        request.ReceiveAllLocations);'''

content = content.replace(old_create, new_create)

# Write the fixed content
with open(file_path, 'w', encoding='utf-8', newline='\n') as f:
    f.write(content)

print("Fixed SubscribeToNewsletterCommandHandler.cs")
print("Changes applied:")
print("  1. Added using statements (LankaConnect.Domain.Events, Microsoft.EntityFrameworkCore)")
print("  2. Added IApplicationDbContext injection")
print("  3. Added PopulateMetroAreasIfNeededAsync() helper method")
print("  4. Fixed reactivate path to populate metros when ReceiveAllLocations=true")
print("  5. Fixed new subscriber path to populate metros when ReceiveAllLocations=true")
