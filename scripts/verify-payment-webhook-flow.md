# Payment Webhook Flow Verification Script

## DEPLOYED STATE (Commit d264a82 - 2025-12-17 12:51 UTC)

### Files Deployed:
1. ✅ `AppDbContext.cs` - IPublisher injection + domain event dispatching in CommitAsync
2. ✅ `PaymentCompletedEventHandler.cs` - Handler for PaymentCompletedEvent
3. ✅ `Registration.cs` - CompletePayment() raises PaymentCompletedEvent
4. ✅ `UnitOfWork.cs` - Calls _context.CommitAsync()
5. ✅ `PaymentsController.cs` - HandleCheckoutSessionCompletedAsync calls CompletePayment

### Expected Flow:
```
1. Stripe sends webhook → POST /api/payments/webhook
2. PaymentsController.Webhook() validates signature
3. Routes to HandleCheckoutSessionCompletedAsync()
4. Calls registration.CompletePayment(paymentIntentId)
   → Sets PaymentStatus = Completed
   → Sets Status = Confirmed
   → RaiseDomainEvent(new PaymentCompletedEvent(...))
5. Calls _unitOfWork.CommitAsync()
   → UnitOfWork.CommitAsync() calls _context.CommitAsync()
   → AppDbContext.CommitAsync() collects domain events
   → Saves changes to DB
   → Dispatches events via _publisher.Publish()
6. MediatR routes to PaymentCompletedEventHandler.Handle()
7. Handler generates ticket + sends email
```

### What SHOULD Appear in Logs:
```
1. "Processing webhook event {EventId} of type checkout.session.completed"
2. "Processing checkout.session.completed for session {SessionId}, payment status paid"
3. "Completing payment for Event {EventId}, Registration {RegistrationId}"
4. "[Phase 6A.24] Found {Count} domain events to dispatch: PaymentCompletedEvent"
5. "[Phase 6A.24] Dispatching domain event: PaymentCompletedEvent"
6. "[Phase 6A.24] Successfully dispatched domain event: PaymentCompletedEvent"
7. "[Phase 6A.24] ✅ PaymentCompletedEventHandler INVOKED - Event {EventId}, Registration {RegistrationId}"
8. "Successfully completed payment for Event {EventId}, Registration {RegistrationId}"
```

### What Actually Happened (Based on User Report):
❌ NO logs after line 3 "Completing payment for Event..."
❌ NO domain event dispatching logs
❌ NO PaymentCompletedEventHandler logs
❌ NO email sent
❌ NO ticket generated

## ROOT CAUSE HYPOTHESIS

### Theory 1: Exception in HandleCheckoutSessionCompletedAsync (MOST LIKELY)
**Evidence:**
- Log line 3 appears: "Completing payment for Event {EventId}, Registration {RegistrationId}"
- NO subsequent logs
- Webhook returns 200 OK (based on Stripe not retrying)

**Issue:**
The webhook handler has a try-catch that logs exceptions but the exception might be:
1. Swallowed before reaching CommitAsync
2. Happening in the Event or Registration retrieval
3. Occurring in CompletePayment() validation

**Fix:**
Add enhanced logging before EVERY step in HandleCheckoutSessionCompletedAsync

### Theory 2: CommitAsync Not Being Called
**Evidence:**
- No domain event logs appear
- CommitAsync has logging that should show "[Phase 6A.24] Found {Count} domain events"

**Issue:**
- Early return from HandleCheckoutSessionCompletedAsync before CommitAsync
- Exception before CommitAsync that gets caught

**Fix:**
Log immediately before CommitAsync call

### Theory 3: Domain Events Not Being Raised
**Evidence:**
- CompletePayment() should call RaiseDomainEvent()
- No logs showing domain events collected

**Issue:**
- Domain event not being added to entity's DomainEvents collection
- ChangeTracker not tracking the modified entity

**Fix:**
Verify entity is tracked by EF Core before CommitAsync

## DIAGNOSTIC STEPS

### Step 1: Add Logging to PaymentsController.HandleCheckoutSessionCompletedAsync

Add logs at these exact points:
```csharp
_logger.LogInformation("STEP 1: Retrieved session data for {SessionId}", session.Id);
_logger.LogInformation("STEP 2: Extracted metadata - EventId: {EventId}, RegistrationId: {RegistrationId}", eventId, registrationId);
_logger.LogInformation("STEP 3: Retrieved event {EventId}", eventId);
_logger.LogInformation("STEP 4: Found registration {RegistrationId}", registrationId);
_logger.LogInformation("STEP 5: About to call CompletePayment with intent {PaymentIntentId}", paymentIntentId);

var completeResult = registration.CompletePayment(paymentIntentId);

_logger.LogInformation("STEP 6: CompletePayment result - Success: {IsSuccess}, Error: {Error}",
    completeResult.IsSuccess, completeResult.Error ?? "None");

// CHECK IF DOMAIN EVENT WAS RAISED
_logger.LogInformation("STEP 7: Domain events count on registration: {Count}", registration.DomainEvents.Count);
_logger.LogInformation("STEP 8: About to call CommitAsync");

await _unitOfWork.CommitAsync();

_logger.LogInformation("STEP 9: CommitAsync completed successfully");
```

### Step 2: Check Current Deployed Logs

Run this query:
```bash
az containerapp logs show \
  --name lankaconnect-staging \
  --resource-group rg-lankaconnect-staging \
  --follow \
  --tail 50 \
  | grep -E "checkout.session.completed|CompletePayment|STEP [1-9]|PaymentCompleted"
```

### Step 3: Trigger Test Payment

Use the test script to create a payment and see EXACTLY where it fails.

## EXPECTED FINDINGS

If logs show:
- STEP 1-6 but NOT STEP 7 with count > 0 → Domain event not being raised
- STEP 1-7 but NOT STEP 8 → Early return or exception
- STEP 1-8 but NOT STEP 9 → CommitAsync failing silently
- STEP 9 appears but no PaymentCompletedEventHandler logs → MediatR not dispatching
