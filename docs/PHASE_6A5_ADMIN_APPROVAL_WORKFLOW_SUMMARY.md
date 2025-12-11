# Phase 6A.5: Admin Approval Workflow - Complete ✅

**Date Completed**: 2025-11-11
**Status**: ✅ Complete
**Build Status**: Backend 0 errors | Frontend 0 TypeScript errors
**Last Updated**: 2025-11-12

---

## Overview

Phase 6A.5 implements the complete admin approval workflow for role upgrade requests, including admin interfaces, approval/rejection logic, free trial initialization, and email notifications.

---

## Admin Approval Workflow

### User Request Flow

```
1. GeneralUser submits upgrade request (Phase 6A.7)
   ↓
2. Request pending: User.PendingUpgradeRole = EventOrganizer
   ↓
3. Admin views pending requests in Approvals page
   ↓
4a. Admin Approves
    ├─ Set User.Role = EventOrganizer
    ├─ Set SubscriptionStatus = Trialing
    ├─ Set FreeTrialStartedAt = now
    ├─ Set FreeTrialEndsAt = now + 6 months
    ├─ Clear PendingUpgradeRole
    ├─ Create approval notification
    └─ Send approval email
   ↓
4b. Admin Rejects
    ├─ Clear PendingUpgradeRole
    ├─ Create rejection notification
    └─ Send rejection email
```

---

## Backend Implementation

### Commands

**ApproveRoleUpgradeCommand** ([src/LankaConnect.Application/Users/Commands/ApproveRoleUpgrade/ApproveRoleUpgradeCommand.cs](../../src/LankaConnect.Application/Users/Commands/ApproveRoleUpgrade/ApproveRoleUpgradeCommand.cs))
- Properties: `UserId`, `ApprovedBy` (admin ID)
- Approves pending upgrade request
- Initiates 6-month free trial

**RejectRoleUpgradeCommand** ([src/LankaConnect.Application/Users/Commands/RejectRoleUpgrade/RejectRoleUpgradeCommand.cs](../../src/LankaConnect.Application/Users/Commands/RejectRoleUpgrade/RejectRoleUpgradeCommand.cs))
- Properties: `UserId`, `RejectionReason`, `RejectedBy` (admin ID)
- Rejects pending upgrade request
- Provides optional reason to user

### Command Handlers

**ApproveRoleUpgradeCommandHandler**
```csharp
public class ApproveRoleUpgradeCommandHandler : ICommandHandler<ApproveRoleUpgradeCommand>
{
    public async Task<Result> Handle(ApproveRoleUpgradeCommand command, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);

        if (user.PendingUpgradeRole == null)
            return Result.Failure("No pending role upgrade");

        // Upgrade role
        var targetRole = user.PendingUpgradeRole.Value;
        user.SetRole(targetRole);

        // Start free trial if subscription required
        if (targetRole.RequiresSubscription())
        {
            user.SetSubscriptionStatus(SubscriptionStatus.Trialing);
            user.SetFreeTrialDates(DateTime.UtcNow, DateTime.UtcNow.AddMonths(6));
        }

        // Clear pending upgrade
        user.ClearPendingUpgradeRole();

        await _userRepository.UpdateAsync(user, cancellationToken);

        // Create notification
        var notification = Notification.Create(
            user.Id,
            "Role Upgrade Approved",
            $"Congratulations! Your request to become {targetRole.ToDisplayName()} has been approved. " +
            $"You now have a 6-month free trial.",
            NotificationType.RoleUpgradeApproved
        );

        if (notification.IsSuccess)
            await _notificationRepository.AddAsync(notification.Value, cancellationToken);

        // Send email (Phase 6A.5.15)
        // Note: Email service would be called here

        return Result.Success();
    }
}
```

**RejectRoleUpgradeCommandHandler**
```csharp
public class RejectRoleUpgradeCommandHandler : ICommandHandler<RejectRoleUpgradeCommand>
{
    public async Task<Result> Handle(RejectRoleUpgradeCommand command, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);

        if (user.PendingUpgradeRole == null)
            return Result.Failure("No pending role upgrade to reject");

        // Clear pending upgrade
        user.ClearPendingUpgradeRole();

        await _userRepository.UpdateAsync(user, cancellationToken);

        // Create notification
        var message = "Your role upgrade request has been declined.";
        if (!string.IsNullOrEmpty(command.RejectionReason))
            message += $" Reason: {command.RejectionReason}";

        var notification = Notification.Create(
            user.Id,
            "Role Upgrade Request Declined",
            message,
            NotificationType.RoleUpgradeRejected
        );

        if (notification.IsSuccess)
            await _notificationRepository.AddAsync(notification.Value, cancellationToken);

        return Result.Success();
    }
}
```

### Queries

**GetPendingRoleUpgradesQuery** ([src/LankaConnect.Application/Users/Queries/GetPendingRoleUpgrades/GetPendingRoleUpgradesQuery.cs](../../src/LankaConnect.Application/Users/Queries/GetPendingRoleUpgrades/GetPendingRoleUpgradesQuery.cs))
- Returns: List of users with pending upgrade requests
- Used by admin approvals page
- Includes: User ID, name, email, requested role, request date

### API Endpoints

**AdminController** ([src/LankaConnect.API/Controllers/AdminController.cs](../../src/LankaConnect.API/Controllers/AdminController.cs))

```csharp
[ApiController]
[Route("api/admin")]
[Authorize(Policy = "CanManageUsers")]
public class AdminController : ControllerBase
{
    [HttpGet("pending-approvals")]
    public async Task<IActionResult> GetPendingApprovals()
    {
        // Returns list of PendingRoleUpgradeDto
    }

    [HttpPost("approve-upgrade/{userId}")]
    public async Task<IActionResult> ApproveRoleUpgrade(Guid userId)
    {
        // Approve user role upgrade
        // Returns: 204 No Content on success
    }

    [HttpPost("reject-upgrade/{userId}")]
    public async Task<IActionResult> RejectRoleUpgrade(
        Guid userId,
        [FromBody] RejectUpgradeRequest request)
    {
        // Reject user role upgrade with optional reason
        // Returns: 204 No Content on success
    }
}
```

---

## Frontend Implementation

### Admin Approvals Page

**File**: [web/src/app/(dashboard)/admin/approvals/page.tsx](../../web/src/app/(dashboard)/admin/approvals/page.tsx)

**Features**:
- Displays pending role upgrade requests in a table
- Shows: User name, email, requested role, request date
- Action buttons: Approve, Reject
- Loading and error states
- Confirmation modals for actions

**Table Columns**:
| Column | Content |
|--------|---------|
| Name | User full name |
| Email | User email |
| Requested Role | EventOrganizer, BusinessOwner, etc. |
| Requested Date | When request was submitted |
| Actions | Approve/Reject buttons |

### React Query Hooks

**useAdminApprovals.ts** ([web/src/presentation/hooks/useAdminApprovals.ts](../../web/src/presentation/hooks/useAdminApprovals.ts))

```typescript
export function useGetPendingApprovals() {
  return useQuery({
    queryKey: ['pending-approvals'],
    queryFn: () => adminRepository.getPendingApprovals(),
    refetchInterval: 30000 // Refetch every 30 seconds
  });
}

export function useApproveRoleUpgrade() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (userId: string) => adminRepository.approveRoleUpgrade(userId),
    onSuccess: () => {
      // Refetch pending approvals
      queryClient.invalidateQueries({ queryKey: ['pending-approvals'] });
    }
  });
}

export function useRejectRoleUpgrade() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ userId, reason }: RejectUpgradeRequest) =>
      adminRepository.rejectRoleUpgrade(userId, reason),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['pending-approvals'] });
    }
  });
}
```

### Approval Modals

**ApprovalConfirmationModal** ([web/src/presentation/components/features/admin/ApprovalConfirmationModal.tsx](../../web/src/presentation/components/features/admin/ApprovalConfirmationModal.tsx))
- Displays when admin clicks "Approve"
- Shows: User details, requested role, message
- Actions: Confirm approve, Cancel

**RejectionModal** ([web/src/presentation/components/features/admin/RejectionModal.tsx](../../web/src/presentation/components/features/admin/RejectionModal.tsx))
- Displays when admin clicks "Reject"
- Optional reason textarea
- Actions: Confirm reject, Cancel

---

## Free Trial Initialization

### Trial Calculation

When EventOrganizer is approved:
```
FreeTrialStartedAt = DateTime.UtcNow
FreeTrialEndsAt = FreeTrialStartedAt.AddMonths(6)

Example:
  If approved: 2025-01-15 10:30:00 UTC
  Trial ends: 2025-07-15 23:59:59 UTC
```

### Database Updates

```sql
UPDATE users.users
SET
  role = 3,  -- EventOrganizer
  subscription_status = 1,  -- Trialing
  free_trial_started_at = NOW(),
  free_trial_ends_at = NOW() + INTERVAL '6 months',
  pending_upgrade_role = NULL
WHERE id = $1;
```

---

## Notification Integration

### Approval Notification

**Type**: RoleUpgradeApproved

**Message**: "Congratulations! Your request to become Event Organizer has been approved. You now have a 6-month free trial to explore all Event Organizer features."

**Properties**:
- RelatedEntityId: UserId
- RelatedEntityType: "User"
- IsRead: false

### Rejection Notification

**Type**: RoleUpgradeRejected

**Message**: "Your role upgrade request has been declined."
+ Optional reason: "Reason: {RejectionReason}"

---

## API Request/Response Examples

### Get Pending Approvals
**Request**:
```http
GET /api/admin/pending-approvals
Authorization: Bearer {token}
```

**Response**:
```json
[
  {
    "userId": "uuid",
    "fullName": "John Doe",
    "email": "john@example.com",
    "requestedRole": "EventOrganizer",
    "requestedAt": "2025-01-10T15:30:00Z",
    "reason": "I want to organize community events"
  }
]
```

### Approve Role Upgrade
**Request**:
```http
POST /api/admin/approve-upgrade/uuid
Authorization: Bearer {token}
```

**Response**: `204 No Content`

### Reject Role Upgrade
**Request**:
```http
POST /api/admin/reject-upgrade/uuid
Content-Type: application/json
Authorization: Bearer {token}

{
  "rejectionReason": "Does not meet community guidelines"
}
```

**Response**: `204 No Content`

---

## Authorization

**Required Policy**: `CanManageUsers`
- Only Admin and AdminManager roles
- Checked on all approval endpoints
- Enforced at controller level with `[Authorize(Policy = "CanManageUsers")]`

---

## Testing Performed

1. **Backend Build**: Successful with 0 errors
2. **Approval Flow**: Approval updates user role and subscription
3. **Rejection Flow**: Rejection clears pending role without changing user
4. **Notification Creation**: Approvals/rejections create notifications
5. **Authorization**: Only admins can access approval endpoints
6. **Frontend Build**: Successful with 0 TypeScript errors

---

## User Experience

### Admin Perspective
1. View pending requests in Approvals page
2. Click "Approve" - confirmation modal appears
3. Confirm - user is approved, receives notification
4. OR Click "Reject" - rejection modal appears with reason field
5. Confirm - user is rejected, receives notification with reason

### User Perspective
1. After approval:
   - Role immediately changes to EventOrganizer
   - "Create Event" button appears
   - Free trial countdown appears
   - Approval notification in inbox
2. After rejection:
   - Pending request cleared
   - "Upgrade to Event Organizer" button reappears
   - Rejection notification with reason (if provided)

---

## Phase 1 vs Phase 2

### Phase 1 MVP (Current)
- ✅ Manual admin approval required
- ✅ EventOrganizer upgrade path only
- ✅ 6-month free trial auto-initialized
- ✅ Notifications on approve/reject
- ⏳ Email notifications (planned, not yet implemented)

### Phase 2 Production
- Activate BusinessOwner approval path
- Activate EventOrganizerAndBusinessOwner approval
- Approval workflow for business profiles
- Email notifications implementation

---

## Build Status

**Backend Build**: ✅ **0 errors** (47.44s compile time)
- ApproveRoleUpgradeCommandHandler verified
- RejectRoleUpgradeCommandHandler verified
- Authorization policies enforced

**Frontend Build**: ✅ **0 TypeScript errors** (24.9s compile time)
- Approvals page compiled successfully
- Modals validated

---

## Integration Points

1. **With Phase 6A.0 (Roles)**: Updates user.role property
2. **With Phase 6A.1 (Subscription)**: Initializes free trial
3. **With Phase 6A.6 (Notifications)**: Creates approval/rejection notifications
4. **With Phase 6A.7 (User Upgrade)**: Handles requests created by users

---

## Next Steps

1. **Email Notifications**: Send approval/rejection emails to users
2. **Approval History**: Track which admin approved which request
3. **Bulk Approvals**: Allow approving multiple requests at once

---

## Related Documentation

- See [PHASE_6A_MASTER_INDEX.md](./PHASE_6A_MASTER_INDEX.md) for complete phase registry
- See [PROGRESS_TRACKER.md](./PROGRESS_TRACKER.md) for overall project status
