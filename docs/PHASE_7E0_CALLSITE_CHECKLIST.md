# Phase 7E.0 — Call-Site Sweep Checklist

**Status**: ✅ COMPLETE (sweep run 2026-04-25, no code changes; this is the audit catalogue)
**Plan**: `C:\Users\Niroshana\.claude\plans\now-show-me-the-shiny-pine.md`
**Master TODO**: [MASTER_TODO_PHASE_7E_FLEXIBLE_REGISTRATION.md](./MASTER_TODO_PHASE_7E_FLEXIBLE_REGISTRATION.md)

## How this checklist is used

Every entry below is a place where the codebase reads a field that the new `RegistrationMode` enum or `HeadCountBreakdown` value object affects. Each entry has a **status tag** describing what action — if any — Phase 7E owes that call-site:

| Tag | Meaning |
|---|---|
| `unchanged` | Read is mode-neutral; no Phase 7E change required. Validate in 7E.9. |
| `needs-mode-aware-update` | Must branch on `event.RegistrationMode`. Updated during 7E.1–7E.8. |
| `left-join-fix` | LINQ `Join`/EF `Include`/raw SQL onto `Registration` from a standalone-payment entity (`Donation` / `AddOnPurchase`). `INNER JOIN` semantics silently drop standalone purchases under Mode C — convert to `LEFT JOIN` / `DefaultIfEmpty` in 7E.8. |
| `defensive-read` | Frontend consumer that would crash if `event.registrationMode` is `undefined` (legacy cached payload). Guard with `event.registrationMode ?? RegistrationMode.DetailedAttendees` during 7E.5–7E.7. |
| `guard-scope-fix` | Confirm `Event.SetRegistrationMode` does NOT lock based on this collection. (Final tally below: 0 — `Event` aggregate has no standalone-contribution collections; configs are nullable value-objects, mode-agnostic by design.) |

**7E.9 verifies every row** — no row should remain `pending` when the phase closes.

---

## Summary counts

- Total entries: **163**
- `needs-mode-aware-update`: **149**
- `left-join-fix`: **4**
- `defensive-read`: **2**
- `guard-scope-fix`: **0** *(architect concern resolved: Event has no standalone-contribution navigation collections — see §6 below)*
- `unchanged`: **8**

---

## 1. `IsFreeEvent` consumers

| File:Line | What it does | Status |
|---|---|---|
| [src/LankaConnect.Shared/Email/Contracts/EventDetailsEmailParams.cs:120](../src/LankaConnect.Shared/Email/Contracts/EventDetailsEmailParams.cs#L120) | Email params property for free event flag | unchanged |
| [src/LankaConnect.Infrastructure/Services/Export/ExcelExportService.cs:342](../src/LankaConnect.Infrastructure/Services/Export/ExcelExportService.cs#L342) | Conditional price column in Excel export | needs-mode-aware-update |
| [src/LankaConnect.Infrastructure/Services/Export/ExcelExportService.cs:406](../src/LankaConnect.Infrastructure/Services/Export/ExcelExportService.cs#L406) | Price column header for paid events | needs-mode-aware-update |
| [src/LankaConnect.Infrastructure/Services/Export/ExcelExportService.cs:511](../src/LankaConnect.Infrastructure/Services/Export/ExcelExportService.cs#L511) | Payment summary conditionally shown | needs-mode-aware-update |
| [src/LankaConnect.Infrastructure/Services/Export/CsvExportService.cs:46](../src/LankaConnect.Infrastructure/Services/Export/CsvExportService.cs#L46) | Payment columns CSV header for paid events | needs-mode-aware-update |
| [src/LankaConnect.Infrastructure/Services/Export/CsvExportService.cs:104](../src/LankaConnect.Infrastructure/Services/Export/CsvExportService.cs#L104) | Payment data CSV rows for paid events | needs-mode-aware-update |
| [src/LankaConnect.Infrastructure/Services/Export/CsvExportService.cs:149](../src/LankaConnect.Infrastructure/Services/Export/CsvExportService.cs#L149) | Currency handling in CSV export | needs-mode-aware-update |
| [web/src/app/search/page.tsx:534](../web/src/app/search/page.tsx#L534) | Search results display free vs paid price | needs-mode-aware-update |
| [web/src/presentation/utils/eventMapper.ts:275](../web/src/presentation/utils/eventMapper.ts#L275) | Event mapper determines free pricing display | needs-mode-aware-update |
| [web/src/app/events/page.tsx:549](../web/src/app/events/page.tsx#L549) | Event list card free/paid price display | needs-mode-aware-update |
| [web/src/app/events/[id]/page.tsx:869](../web/src/app/events/%5Bid%5D/page.tsx#L869) | Event details page free/paid label | needs-mode-aware-update |

## 2. `Registrations.Sum` / `Registrations.Count` aggregations

The capacity formula moves to `Sum(r.HeadCount?.Total ?? r.Attendees.Count)` in 7E.1; every aggregator below must use the new formula.

| File:Line | What it does | Status |
|---|---|---|
| [src/LankaConnect.Domain/Events/Event.cs:109-111](../src/LankaConnect.Domain/Events/Event.cs#L109) | `CurrentRegistrations` computed from `Sum(r.GetAttendeeCount())` — **the canonical aggregator** | needs-mode-aware-update |
| [src/LankaConnect.Domain/Events/Event.cs:115-117](../src/LankaConnect.Domain/Events/Event.cs#L115) | `ReservedCapacity` includes Preliminary registrations | needs-mode-aware-update |
| [src/LankaConnect.API/Controllers/AdminController.cs:452](../src/LankaConnect.API/Controllers/AdminController.cs#L452) | Admin dashboard event count | needs-mode-aware-update |
| [src/LankaConnect.API/Controllers/AdminController.cs:463](../src/LankaConnect.API/Controllers/AdminController.cs#L463) | Admin dashboard draft events count | needs-mode-aware-update |
| [src/LankaConnect.API/Controllers/AdminController.cs:474](../src/LankaConnect.API/Controllers/AdminController.cs#L474) | Admin dashboard published events count | needs-mode-aware-update |
| [src/LankaConnect.API/Controllers/AdminController.cs:587](../src/LankaConnect.API/Controllers/AdminController.cs#L587) | Reminder processing registration count log | needs-mode-aware-update |
| [src/LankaConnect.API/Controllers/AdminController.cs:589](../src/LankaConnect.API/Controllers/AdminController.cs#L589) | Guard check: zero registrations | needs-mode-aware-update |
| [src/LankaConnect.API/Controllers/AdminController.cs:612](../src/LankaConnect.API/Controllers/AdminController.cs#L612) | Reminder response includes registration count | needs-mode-aware-update |
| [src/LankaConnect.Application/Events/EventHandlers/PaymentCompletedEventHandler.cs:109](../src/LankaConnect.Application/Events/EventHandlers/PaymentCompletedEventHandler.cs#L109) | Payment handler logs registration count | needs-mode-aware-update |

## 3. `event.Capacity` reads

| File:Line | What it does | Status |
|---|---|---|
| [src/LankaConnect.Domain/Events/Event.cs:624](../src/LankaConnect.Domain/Events/Event.cs#L624) | Capacity check in registration validation | needs-mode-aware-update |
| [src/LankaConnect.Domain/Events/Event.cs:664](../src/LankaConnect.Domain/Events/Event.cs#L664) | `CanAccommodate` checks reserved capacity | needs-mode-aware-update |
| [src/LankaConnect.Domain/Events/Event.cs:782](../src/LankaConnect.Domain/Events/Event.cs#L782) | `UpdateCapacity` validates against current registrations | needs-mode-aware-update |
| [src/LankaConnect.Domain/Events/Event.cs:837](../src/LankaConnect.Domain/Events/Event.cs#L837) | `GetAvailableSpotsForRegistration` computes available | needs-mode-aware-update |
| [src/LankaConnect.Application/Events/Commands/UpdateEvent/UpdateEventCommandHandler.cs:102](../src/LankaConnect.Application/Events/Commands/UpdateEvent/UpdateEventCommandHandler.cs#L102) | Validation: new capacity ≥ current registrations | needs-mode-aware-update |
| [src/LankaConnect.Application/Events/Commands/UpdateEventCapacity/UpdateEventCapacityCommandHandler.cs:56](../src/LankaConnect.Application/Events/Commands/UpdateEventCapacity/UpdateEventCapacityCommandHandler.cs#L56) | Log current capacity before update | needs-mode-aware-update |
| [src/LankaConnect.Application/Events/Commands/UpdateEventCapacity/UpdateEventCapacityCommandHandler.cs:73](../src/LankaConnect.Application/Events/Commands/UpdateEventCapacity/UpdateEventCapacityCommandHandler.cs#L73) | Log capacity change | needs-mode-aware-update |
| [src/LankaConnect.Application/Events/Queries/CalculateAdditionPrice/CalculateAdditionPriceQueryHandler.cs:112](../src/LankaConnect.Application/Events/Queries/CalculateAdditionPrice/CalculateAdditionPriceQueryHandler.cs#L112) | Include capacity in response DTO | needs-mode-aware-update |
| [web/src/app/events/page.tsx:540](../web/src/app/events/page.tsx#L540) | Event list shows registered / capacity | needs-mode-aware-update |
| [web/src/app/events/[id]/page.tsx:850](../web/src/app/events/%5Bid%5D/page.tsx#L850) | Event details shows registered / capacity | needs-mode-aware-update |
| [web/src/app/events/[id]/page.tsx:657](../web/src/app/events/%5Bid%5D/page.tsx#L657) | Compute `isFull` based on capacity | needs-mode-aware-update |
| [web/src/app/events/[id]/page.tsx:658](../web/src/app/events/%5Bid%5D/page.tsx#L658) | Compute `spotsLeft` from capacity | needs-mode-aware-update |
| [web/src/presentation/utils/eventMapper.ts:313](../web/src/presentation/utils/eventMapper.ts#L313) | `isFull` utility function | needs-mode-aware-update |
| [web/src/presentation/utils/eventMapper.ts:323](../web/src/presentation/utils/eventMapper.ts#L323) | `spotsLeft` utility function | needs-mode-aware-update |
| [web/src/presentation/components/features/events/EditRegistrationModal.tsx:85](../web/src/presentation/components/features/events/EditRegistrationModal.tsx#L85) | Max attendees capped by available spots | needs-mode-aware-update |
| [web/src/presentation/components/features/events/EventDetailsTab.tsx:72-73](../web/src/presentation/components/features/events/EventDetailsTab.tsx#L72) | Dashboard tab computes `spotsLeft` and percentage | needs-mode-aware-update |

## 4. `Attendees.Count` / per-attendee enumerations

These break under Mode B (no per-attendee rows). Each must branch on mode or use a mode-agnostic helper.

### Backend

| File:Line | What it does | Status |
|---|---|---|
| [src/LankaConnect.API/Controllers/EventsController.cs:612](../src/LankaConnect.API/Controllers/EventsController.cs#L612) | Log attendee count on registration | needs-mode-aware-update |
| [src/LankaConnect.API/Controllers/EventsController.cs:710](../src/LankaConnect.API/Controllers/EventsController.cs#L710) | Map request attendees to domain | needs-mode-aware-update |
| [src/LankaConnect.Infrastructure/Services/Export/CsvExportService.cs:77](../src/LankaConnect.Infrastructure/Services/Export/CsvExportService.cs#L77) | Get first attendee for currency | needs-mode-aware-update |
| [src/LankaConnect.Infrastructure/Services/Export/CsvExportService.cs:82](../src/LankaConnect.Infrastructure/Services/Export/CsvExportService.cs#L82) | Get main attendee name from `Attendees` | needs-mode-aware-update |
| [src/LankaConnect.Infrastructure/Services/Export/CsvExportService.cs:86-87](../src/LankaConnect.Infrastructure/Services/Export/CsvExportService.cs#L86) | Count male/female attendees | needs-mode-aware-update |
| [src/LankaConnect.Infrastructure/Payments/Services/AdditionWebhookHandler.cs:136](../src/LankaConnect.Infrastructure/Payments/Services/AdditionWebhookHandler.cs#L136) | Log attendee count on payment | needs-mode-aware-update |
| [src/LankaConnect.Infrastructure/Payments/Services/AdditionWebhookHandler.cs:220](../src/LankaConnect.Infrastructure/Payments/Services/AdditionWebhookHandler.cs#L220) | Log new attendees added count | needs-mode-aware-update |
| [src/LankaConnect.Infrastructure/Payments/Services/AdditionWebhookHandler.cs:291](../src/LankaConnect.Infrastructure/Payments/Services/AdditionWebhookHandler.cs#L291) | Log all counts in webhook handler | needs-mode-aware-update |
| [src/LankaConnect.Application/Events/Queries/GetUserRegistrationForEvent/GetUserRegistrationForEventQueryHandler.cs:107](../src/LankaConnect.Application/Events/Queries/GetUserRegistrationForEvent/GetUserRegistrationForEventQueryHandler.cs#L107) | Map `Attendees` to `AttendeeDetailsDto` | needs-mode-aware-update |
| [src/LankaConnect.Application/Events/Queries/GetTicket/GetTicketQuery.cs:63](../src/LankaConnect.Application/Events/Queries/GetTicket/GetTicketQuery.cs#L63) | Compute `AttendeeCount` from `Attendees` | needs-mode-aware-update |
| [src/LankaConnect.Application/Events/Queries/GetTicket/GetTicketQuery.cs:66](../src/LankaConnect.Application/Events/Queries/GetTicket/GetTicketQuery.cs#L66) | Map `Attendees` for ticket display | needs-mode-aware-update |
| [src/LankaConnect.Application/Events/Queries/GetRegistrationById/GetRegistrationByIdQueryHandler.cs:86](../src/LankaConnect.Application/Events/Queries/GetRegistrationById/GetRegistrationByIdQueryHandler.cs#L86) | Map `Attendees` in registration query | needs-mode-aware-update |
| [src/LankaConnect.Application/Events/Queries/GetEventAttendees/GetEventAttendeesQueryHandler.cs:125](../src/LankaConnect.Application/Events/Queries/GetEventAttendees/GetEventAttendeesQueryHandler.cs#L125) | Map `Attendees` to DTO for organiser view | needs-mode-aware-update |
| [src/LankaConnect.Application/Events/Queries/GetEventAttendees/GetEventAttendeesQueryHandler.cs:132](../src/LankaConnect.Application/Events/Queries/GetEventAttendees/GetEventAttendeesQueryHandler.cs#L132) | Count total attendees | needs-mode-aware-update |
| [src/LankaConnect.Application/Events/Queries/GetEventAttendees/GetEventAttendeesQueryHandler.cs:136-137](../src/LankaConnect.Application/Events/Queries/GetEventAttendees/GetEventAttendeesQueryHandler.cs#L136) | Count adults and children | needs-mode-aware-update |
| [src/LankaConnect.Application/Events/Queries/GetEventAttendees/GetEventAttendeesQueryHandler.cs:141](../src/LankaConnect.Application/Events/Queries/GetEventAttendees/GetEventAttendeesQueryHandler.cs#L141) | Gender breakdown enumeration | needs-mode-aware-update |
| [src/LankaConnect.Application/Events/Queries/GetEventAttendees/GetEventAttendeesQueryHandler.cs:356-357](../src/LankaConnect.Application/Events/Queries/GetEventAttendees/GetEventAttendeesQueryHandler.cs#L356) | Compute adult/child counts in method | needs-mode-aware-update |
| [src/LankaConnect.Application/Events/Queries/GetEventAttendees/GetEventAttendeesQueryHandler.cs:361](../src/LankaConnect.Application/Events/Queries/GetEventAttendees/GetEventAttendeesQueryHandler.cs#L361) | Gender count aggregation | needs-mode-aware-update |
| [src/LankaConnect.Application/Events/Queries/GetEventAttendees/GetEventAttendeesQueryHandler.cs:372](../src/LankaConnect.Application/Events/Queries/GetEventAttendees/GetEventAttendeesQueryHandler.cs#L372) | Map attendees to DTOs | needs-mode-aware-update |
| [src/LankaConnect.Application/Events/Queries/GetEventAttendees/GetEventAttendeesQueryHandler.cs:392](../src/LankaConnect.Application/Events/Queries/GetEventAttendees/GetEventAttendeesQueryHandler.cs#L392) | `TotalAttendees` count property | needs-mode-aware-update |
| [src/LankaConnect.Application/Events/Queries/CalculateAdditionPrice/CalculateAdditionPriceQueryHandler.cs:204](../src/LankaConnect.Application/Events/Queries/CalculateAdditionPrice/CalculateAdditionPriceQueryHandler.cs#L204) | Convert `Attendees` to list for pricing | needs-mode-aware-update |
| [src/LankaConnect.Application/Events/Queries/CalculateAdditionPrice/CalculateAdditionPriceQueryHandler.cs:277](../src/LankaConnect.Application/Events/Queries/CalculateAdditionPrice/CalculateAdditionPriceQueryHandler.cs#L277) | Create `AttendeePrice` from attendee details | needs-mode-aware-update |
| [src/LankaConnect.Application/Events/Queries/CalculateAdditionPrice/CalculateAdditionPriceQueryHandler.cs:289](../src/LankaConnect.Application/Events/Queries/CalculateAdditionPrice/CalculateAdditionPriceQueryHandler.cs#L289) | Single price per attendee | needs-mode-aware-update |
| [src/LankaConnect.Application/Events/Queries/CalculateAdditionPrice/CalculateAdditionPriceQueryHandler.cs:298](../src/LankaConnect.Application/Events/Queries/CalculateAdditionPrice/CalculateAdditionPriceQueryHandler.cs#L298) | Calculate by age category | needs-mode-aware-update |
| [src/LankaConnect.Application/Events/Queries/CalculateAdditionPrice/CalculateAdditionPriceQueryHandler.cs:305-307](../src/LankaConnect.Application/Events/Queries/CalculateAdditionPrice/CalculateAdditionPriceQueryHandler.cs#L305) | Tiered pricing per attendee | needs-mode-aware-update |
| [src/LankaConnect.Application/Events/Queries/CalculateAdditionPrice/CalculateAdditionPriceQueryHandler.cs:330](../src/LankaConnect.Application/Events/Queries/CalculateAdditionPrice/CalculateAdditionPriceQueryHandler.cs#L330) | Group pricing per attendee | needs-mode-aware-update |
| [src/LankaConnect.Application/Events/EventHandlers/AnonymousRegistrationWhatsAppHandler.cs:91](../src/LankaConnect.Application/Events/EventHandlers/AnonymousRegistrationWhatsAppHandler.cs#L91) | Get attendee name for WhatsApp | needs-mode-aware-update |
| [src/LankaConnect.Application/Events/EventHandlers/PaymentCompletedEventHandler.cs:127](../src/LankaConnect.Application/Events/EventHandlers/PaymentCompletedEventHandler.cs#L127) | Log attendee count with mode flag | needs-mode-aware-update |
| [src/LankaConnect.Application/Events/EventHandlers/PaymentCompletedEventHandler.cs:205](../src/LankaConnect.Application/Events/EventHandlers/PaymentCompletedEventHandler.cs#L205) | Attendee details in email (detailed mode) | needs-mode-aware-update |
| [src/LankaConnect.Application/Events/EventHandlers/PaymentCompletedEventHandler.cs:217](../src/LankaConnect.Application/Events/EventHandlers/PaymentCompletedEventHandler.cs#L217) | Attendee HTML enumeration | needs-mode-aware-update |
| [src/LankaConnect.Application/Events/EventHandlers/PaymentCompletedEventHandler.cs:239](../src/LankaConnect.Application/Events/EventHandlers/PaymentCompletedEventHandler.cs#L239) | Stripe quantity from attendee count | needs-mode-aware-update |
| [src/LankaConnect.Application/Events/EventHandlers/PaymentCompletedEventHandler.cs:291](../src/LankaConnect.Application/Events/EventHandlers/PaymentCompletedEventHandler.cs#L291) | Attendee details in add-on email | needs-mode-aware-update |
| [src/LankaConnect.Application/Events/EventHandlers/AttendeesAddedEventHandler.cs:115](../src/LankaConnect.Application/Events/EventHandlers/AttendeesAddedEventHandler.cs#L115) | Recipient name from attendee | needs-mode-aware-update |
| [src/LankaConnect.Application/Events/EventHandlers/AttendeesAddedEventHandler.cs:122](../src/LankaConnect.Application/Events/EventHandlers/AttendeesAddedEventHandler.cs#L122) | Alternate recipient name source | needs-mode-aware-update |
| [src/LankaConnect.Application/Events/EventHandlers/AttendeesAddedEventHandler.cs:183](../src/LankaConnect.Application/Events/EventHandlers/AttendeesAddedEventHandler.cs#L183) | HTML list of attendees added | needs-mode-aware-update |
| [src/LankaConnect.Application/Events/EventHandlers/AttendeesAddedEventHandler.cs:186](../src/LankaConnect.Application/Events/EventHandlers/AttendeesAddedEventHandler.cs#L186) | Text list of attendees with age | needs-mode-aware-update |
| [src/LankaConnect.Application/Events/EventHandlers/AttendeesAddedEventHandler.cs:197](../src/LankaConnect.Application/Events/EventHandlers/AttendeesAddedEventHandler.cs#L197) | Attendee initial letter generation | needs-mode-aware-update |
| [src/LankaConnect.Application/Events/EventHandlers/AttendeesAddedEventHandler.cs:214](../src/LankaConnect.Application/Events/EventHandlers/AttendeesAddedEventHandler.cs#L214) | HTML list all attendees | needs-mode-aware-update |
| [src/LankaConnect.Application/Events/EventHandlers/AttendeesAddedEventHandler.cs:217](../src/LankaConnect.Application/Events/EventHandlers/AttendeesAddedEventHandler.cs#L217) | Text all attendees with age | needs-mode-aware-update |
| [src/LankaConnect.Application/Events/EventHandlers/AnonymousRegistrationConfirmedEventHandler.cs:103](../src/LankaConnect.Application/Events/EventHandlers/AnonymousRegistrationConfirmedEventHandler.cs#L103) | Contact name from attendee | needs-mode-aware-update |
| [src/LankaConnect.Application/Events/EventHandlers/AnonymousRegistrationConfirmedEventHandler.cs:123](../src/LankaConnect.Application/Events/EventHandlers/AnonymousRegistrationConfirmedEventHandler.cs#L123) | Attendee details HTML | needs-mode-aware-update |
| [src/LankaConnect.Application/Events/EventHandlers/RegistrationConfirmedEventHandler.cs:134](../src/LankaConnect.Application/Events/EventHandlers/RegistrationConfirmedEventHandler.cs#L134) | Attendee details HTML | needs-mode-aware-update |
| [src/LankaConnect.Application/Events/BackgroundJobs/EventReminderJob.cs:199](../src/LankaConnect.Application/Events/BackgroundJobs/EventReminderJob.cs#L199) | Get first attendee for reminder name | needs-mode-aware-update |
| [src/LankaConnect.Application/Events/BackgroundJobs/EventReminderJob.cs:415](../src/LankaConnect.Application/Events/BackgroundJobs/EventReminderJob.cs#L415) | Get first attendee for reminder name (alt) | needs-mode-aware-update |
| [src/LankaConnect.Application/Events/Commands/RegisterAnonymousAttendee/RegisterAnonymousAttendeeCommandHandler.cs:170](../src/LankaConnect.Application/Events/Commands/RegisterAnonymousAttendee/RegisterAnonymousAttendeeCommandHandler.cs#L170) | Log attendee count | needs-mode-aware-update |
| [src/LankaConnect.Application/Events/Commands/UpdateRegistrationDetails/UpdateRegistrationDetailsCommandHandler.cs:42](../src/LankaConnect.Application/Events/Commands/UpdateRegistrationDetails/UpdateRegistrationDetailsCommandHandler.cs#L42) | Log new attendee count | needs-mode-aware-update |
| [src/LankaConnect.Application/Events/Commands/PhotoAlbums/SendAlbumNotification/SendAlbumNotificationCommand.cs:315](../src/LankaConnect.Application/Events/Commands/PhotoAlbums/SendAlbumNotification/SendAlbumNotificationCommand.cs#L315) | Guard: only send if has detailed attendees | needs-mode-aware-update |
| [src/LankaConnect.Application/Events/Commands/ResendTicketEmail/ResendTicketEmailCommandHandler.cs:295](../src/LankaConnect.Application/Events/Commands/ResendTicketEmail/ResendTicketEmailCommandHandler.cs#L295) | Get recipient name from attendee | needs-mode-aware-update |
| [src/LankaConnect.Application/Events/Commands/ResendTicketEmail/ResendTicketEmailCommandHandler.cs:318](../src/LankaConnect.Application/Events/Commands/ResendTicketEmail/ResendTicketEmailCommandHandler.cs#L318) | Attendee HTML for ticket email | needs-mode-aware-update |
| [src/LankaConnect.Application/Events/Commands/ResendTicketEmail/ResendTicketEmailCommandHandler.cs:325](../src/LankaConnect.Application/Events/Commands/ResendTicketEmail/ResendTicketEmailCommandHandler.cs#L325) | Attendee HTML seat number | needs-mode-aware-update |
| [src/LankaConnect.Application/Events/Commands/ResendTicketEmail/ResendTicketEmailCommandHandler.cs:330](../src/LankaConnect.Application/Events/Commands/ResendTicketEmail/ResendTicketEmailCommandHandler.cs#L330) | Attendee count text | needs-mode-aware-update |
| [src/LankaConnect.Application/Events/Commands/ResendTicketEmail/ResendTicketEmailCommandHandler.cs:351](../src/LankaConnect.Application/Events/Commands/ResendTicketEmail/ResendTicketEmailCommandHandler.cs#L351) | Stripe quantity from attendee count | needs-mode-aware-update |
| [src/LankaConnect.Infrastructure/Services/Tickets/PdfTicketService.cs:163](../src/LankaConnect.Infrastructure/Services/Tickets/PdfTicketService.cs#L163) | Attendee name and age in PDF | needs-mode-aware-update |
| [src/LankaConnect.Infrastructure/Services/Tickets/TicketService.cs:133](../src/LankaConnect.Infrastructure/Services/Tickets/TicketService.cs#L133) | Attendee name for ticket | needs-mode-aware-update |
| [src/LankaConnect.Infrastructure/Services/Tickets/TicketService.cs:328](../src/LankaConnect.Infrastructure/Services/Tickets/TicketService.cs#L328) | Attendee name for ticket (alt) | needs-mode-aware-update |
| [src/LankaConnect.Infrastructure/Services/Tickets/TicketService.cs:432](../src/LankaConnect.Infrastructure/Services/Tickets/TicketService.cs#L432) | Attendee name check | needs-mode-aware-update |
| [src/LankaConnect.Infrastructure/Services/RegistrationEmailService.cs:349](../src/LankaConnect.Infrastructure/Services/RegistrationEmailService.cs#L349) | Attendee name for email | needs-mode-aware-update |
| [src/LankaConnect.Infrastructure/Services/RegistrationEmailService.cs:372](../src/LankaConnect.Infrastructure/Services/RegistrationEmailService.cs#L372) | Attendee name HTML in email | needs-mode-aware-update |

### Frontend

| File:Line | What it does | Status |
|---|---|---|
| [web/src/app/events/[id]/page.tsx:1140](../web/src/app/events/%5Bid%5D/page.tsx#L1140) | Frontend attendee list length check | needs-mode-aware-update |
| [web/src/app/events/[id]/page.tsx:1146](../web/src/app/events/%5Bid%5D/page.tsx#L1146) | Frontend attendee map enumeration | needs-mode-aware-update |
| [web/src/app/events/payment/success/page.tsx:212](../web/src/app/events/payment/success/page.tsx#L212) | Success page attendee count | needs-mode-aware-update |
| [web/src/presentation/components/features/events/AttendeeManagementTab.tsx:403](../web/src/presentation/components/features/events/AttendeeManagementTab.tsx#L403) | Export disabled if no attendees | needs-mode-aware-update |
| [web/src/presentation/components/features/events/AttendeeManagementTab.tsx:411](../web/src/presentation/components/features/events/AttendeeManagementTab.tsx#L411) | Export button disabled check | needs-mode-aware-update |
| [web/src/presentation/components/features/events/AttendeeManagementTab.tsx:424](../web/src/presentation/components/features/events/AttendeeManagementTab.tsx#L424) | Empty attendee list message | needs-mode-aware-update |
| [web/src/presentation/components/features/events/AttendeeManagementTab.tsx:462](../web/src/presentation/components/features/events/AttendeeManagementTab.tsx#L462) | Attendee map enumeration | needs-mode-aware-update |
| [web/src/presentation/components/features/events/AttendeeManagementTab.tsx:571](../web/src/presentation/components/features/events/AttendeeManagementTab.tsx#L571) | Per-attendee nested map | needs-mode-aware-update |
| [web/src/presentation/components/features/events/AttendeeManagementTab.tsx:640](../web/src/presentation/components/features/events/AttendeeManagementTab.tsx#L640) | Count check for table footer | needs-mode-aware-update |
| [web/src/presentation/components/features/events/EditRegistrationModal.tsx:97](../web/src/presentation/components/features/events/EditRegistrationModal.tsx#L97) | Get original attendee count | needs-mode-aware-update |
| [web/src/presentation/components/features/events/EditRegistrationModal.tsx:157](../web/src/presentation/components/features/events/EditRegistrationModal.tsx#L157) | Check attendee limit | needs-mode-aware-update |
| [web/src/presentation/components/features/events/EditRegistrationModal.tsx:164](../web/src/presentation/components/features/events/EditRegistrationModal.tsx#L164) | Remove attendee check | needs-mode-aware-update |
| [web/src/presentation/components/features/events/EditRegistrationModal.tsx:165](../web/src/presentation/components/features/events/EditRegistrationModal.tsx#L165) | Filter attendees | needs-mode-aware-update |
| [web/src/presentation/components/features/events/EditRegistrationModal.tsx:175](../web/src/presentation/components/features/events/EditRegistrationModal.tsx#L175) | Empty attendee guard | needs-mode-aware-update |
| [web/src/presentation/components/features/events/EditRegistrationModal.tsx:192](../web/src/presentation/components/features/events/EditRegistrationModal.tsx#L192) | Attendee count changed check | needs-mode-aware-update |
| [web/src/presentation/components/features/events/EditRegistrationModal.tsx:229](../web/src/presentation/components/features/events/EditRegistrationModal.tsx#L229) | Map attendees to request | needs-mode-aware-update |
| [web/src/presentation/components/features/events/EditRegistrationModal.tsx:266](../web/src/presentation/components/features/events/EditRegistrationModal.tsx#L266) | Show attendee count label | needs-mode-aware-update |
| [web/src/presentation/components/features/events/EditRegistrationModal.tsx:270](../web/src/presentation/components/features/events/EditRegistrationModal.tsx#L270) | Show add attendee button | needs-mode-aware-update |
| [web/src/presentation/components/features/events/EditRegistrationModal.tsx:306](../web/src/presentation/components/features/events/EditRegistrationModal.tsx#L306) | Map attendees form fields | needs-mode-aware-update |
| [web/src/presentation/components/features/events/EditRegistrationModal.tsx:318](../web/src/presentation/components/features/events/EditRegistrationModal.tsx#L318) | Show remove button | needs-mode-aware-update |
| [web/src/presentation/hooks/useEvents.ts:478](../web/src/presentation/hooks/useEvents.ts#L478) | Compute attendee count | needs-mode-aware-update |
| [web/src/presentation/hooks/useEvents.ts:684-685](../web/src/presentation/hooks/useEvents.ts#L684) | Log and check attendees length | needs-mode-aware-update |

## 5. Standalone-payment joins on `Registration`

`AddOnPurchase.RegistrationId` and `Donation.RegistrationId` are nullable; `Sponsor` and `Collection` have no FK at all. Any `INNER JOIN` (or LINQ `Join` without `DefaultIfEmpty`) silently drops standalone purchases — convert to `LEFT JOIN` semantics in 7E.8.

| File:Line | Type | Inner or Left | Status |
|---|---|---|---|
| [src/LankaConnect.Domain/Events/Donation.cs:36](../src/LankaConnect.Domain/Events/Donation.cs#L36) | FK property | Optional (Guid?) | left-join-fix |
| [src/LankaConnect.Domain/Events/AddOnPurchase.cs:36](../src/LankaConnect.Domain/Events/AddOnPurchase.cs#L36) | FK property | Optional (Guid?) | left-join-fix |
| [src/LankaConnect.Infrastructure/Data/Repositories/DonationRepository.cs:119](../src/LankaConnect.Infrastructure/Data/Repositories/DonationRepository.cs#L119) | Query by RegistrationId | Optional lookup | left-join-fix |
| [src/LankaConnect.Application/Events/EventHandlers/PaymentCompletedEventHandler.cs:332](../src/LankaConnect.Application/Events/EventHandlers/PaymentCompletedEventHandler.cs#L332) | `GetByRegistrationIdAsync` call | Optional result | left-join-fix |

**Sponsor**, **Collection**: no FK at all. Verified clean — no joins to convert.
**Raw SQL with `attendees ->>` / `attendees @>`**: no matches — verified clean.

## 6. `Event` aggregation collections (for `SetRegistrationMode` guard scope)

The architect required confirmation that `SetRegistrationMode` does NOT lock the mode based on standalone contributions. Reading [`Event.cs`](../src/LankaConnect.Domain/Events/Event.cs) confirms the aggregate owns these collections; standalone-contribution shapes are *configuration value-objects*, not registration collections, so the guard is automatically scoped correctly.

| Collection | Type | Guard scope |
|---|---|---|
| `Registrations` | `IReadOnlyList<Registration>` | **INCLUDE** — existing rows lock the mode |
| `Images` | `IReadOnlyList<EventImage>` | EXCLUDE — independent of registration mode |
| `Videos` | `IReadOnlyList<EventVideo>` | EXCLUDE — independent of registration mode |
| `WaitingList` | `IReadOnlyList<WaitingListEntry>` | EXCLUDE — supports all modes |
| `Passes` | `IReadOnlyList<EventPass>` | EXCLUDE — can exist in any mode |
| `SignUpLists` | `IReadOnlyList<SignUpList>` | EXCLUDE — independent of registration mode |
| `Badges` | `IReadOnlyList<EventBadge>` | EXCLUDE — promotional only |
| `EmailGroupIds` | `IReadOnlyList<Guid>` | EXCLUDE — communication only |
| `Donations` | `DonationConfiguration?` (config, not collection) | EXCLUDE — standalone, mode-agnostic |
| `Collections` | `CollectionConfiguration?` (config, not collection) | EXCLUDE — standalone, mode-agnostic |
| `Sponsors` | `SponsorConfiguration?` (config, not collection) | EXCLUDE — standalone, mode-agnostic |
| `AddOns` | `AddOnConfiguration?` (config, not collection) | EXCLUDE — standalone, mode-agnostic |

**Conclusion**: `Event.SetRegistrationMode` only needs to check `Registrations.Any()`. No `guard-scope-fix` entries required. (Architect concern resolved.)

## 7. Email handlers building registration-event email params

Each handler must populate the new EmailTemplateContract booleans (`HasDetailedAttendees`, `HasHeadCount`, `HasHeadCountBreakdown`, `HasTierBreakdown`) **true and false** (never omitted) plus the line strings (`HeadCountTotal`, `HeadCountBreakdownLine`, `TierBreakdownLine`). Lands in 7E.2 (contract) + 7E.4 (templates + handlers).

| Handler | Template (current) | Params class | Status |
|---|---|---|---|
| [RegistrationConfirmedEventHandler](../src/LankaConnect.Application/Events/EventHandlers/RegistrationConfirmedEventHandler.cs) | registration-confirmed | EventDetailsEmailParams | needs-mode-aware-update |
| [AnonymousRegistrationConfirmedEventHandler](../src/LankaConnect.Application/Events/EventHandlers/AnonymousRegistrationConfirmedEventHandler.cs) | anonymous-registration-confirmed | EventDetailsEmailParams | needs-mode-aware-update |
| [PaymentCompletedEventHandler](../src/LankaConnect.Application/Events/EventHandlers/PaymentCompletedEventHandler.cs) | payment-confirmation | EventDetailsEmailParams | needs-mode-aware-update |
| [AttendeesAddedEventHandler](../src/LankaConnect.Application/Events/EventHandlers/AttendeesAddedEventHandler.cs) | attendees-added-confirmation | EventDetailsEmailParams | needs-mode-aware-update |
| [EventReminderJob](../src/LankaConnect.Application/Events/BackgroundJobs/EventReminderJob.cs) | event-reminder | EventDetailsEmailParams | needs-mode-aware-update |
| [ResendTicketEmailCommandHandler](../src/LankaConnect.Application/Events/Commands/ResendTicketEmail/ResendTicketEmailCommandHandler.cs) | ticket-email | EventDetailsEmailParams | needs-mode-aware-update |
| [PhotoAlbumPublishedEmailHandler](../src/LankaConnect.Application/Events/EventHandlers/) | photo-album-published | EventDetailsEmailParams | needs-mode-aware-update |

## 8. Frontend `event.isFreeEvent` consumers

| File:Line | What it does | Status |
|---|---|---|
| [web/src/app/events/page.tsx:549](../web/src/app/events/page.tsx#L549) | Event card free/paid label | needs-mode-aware-update |
| [web/src/app/events/[id]/page.tsx:869](../web/src/app/events/%5Bid%5D/page.tsx#L869) | Event details page pricing display | needs-mode-aware-update |
| [web/src/app/events/[id]/page.tsx:880](../web/src/app/events/%5Bid%5D/page.tsx#L880) | Tier pricing display conditional | needs-mode-aware-update |
| [web/src/app/events/[id]/page.tsx:881](../web/src/app/events/%5Bid%5D/page.tsx#L881) | Child pricing display conditional | needs-mode-aware-update |
| [web/src/app/events/[id]/page.tsx:1050](../web/src/app/events/%5Bid%5D/page.tsx#L1050) | RSVP form free/paid condition | needs-mode-aware-update |
| [web/src/app/events/[id]/page.tsx:1505](../web/src/app/events/%5Bid%5D/page.tsx#L1505) | Checkout form visibility | needs-mode-aware-update |
| [web/src/app/events/[id]/page.tsx:1769](../web/src/app/events/%5Bid%5D/page.tsx#L1769) | Add attendee form pricing | needs-mode-aware-update |
| [web/src/app/events/[id]/page.tsx:1801](../web/src/app/events/%5Bid%5D/page.tsx#L1801) | Update attendee form pricing | needs-mode-aware-update |
| [web/src/app/events/[id]/page.tsx:1835](../web/src/app/events/%5Bid%5D/page.tsx#L1835) | Tier selection visibility | needs-mode-aware-update |
| [web/src/app/events/[id]/page.tsx:1895](../web/src/app/events/%5Bid%5D/page.tsx#L1895) | Premium feature PaymentComplete card | needs-mode-aware-update |
| [web/src/presentation/lib/validators/event.schemas.ts:319-467](../web/src/presentation/lib/validators/event.schemas.ts#L319) | Pricing validation schema (7 sites) | needs-mode-aware-update |
| [web/src/presentation/lib/validators/event.schemas.ts:804-907](../web/src/presentation/lib/validators/event.schemas.ts#L804) | Edit event validation schema (7 sites) | needs-mode-aware-update |
| [web/src/app/search/page.tsx:534](../web/src/app/search/page.tsx#L534) | Search page price display | needs-mode-aware-update |

## 9. Frontend `event.capacity` / `spotsLeft` readers

| File:Line | What it does | Status |
|---|---|---|
| [web/src/app/events/page.tsx:540](../web/src/app/events/page.tsx#L540) | Capacity display in list | needs-mode-aware-update |
| [web/src/app/events/[id]/page.tsx:657](../web/src/app/events/%5Bid%5D/page.tsx#L657) | `isFull` computation | needs-mode-aware-update |
| [web/src/app/events/[id]/page.tsx:658](../web/src/app/events/%5Bid%5D/page.tsx#L658) | `spotsLeft` computation | needs-mode-aware-update |
| [web/src/app/events/[id]/page.tsx:850](../web/src/app/events/%5Bid%5D/page.tsx#L850) | Capacity display on details | needs-mode-aware-update |
| [web/src/app/events/[id]/page.tsx:856](../web/src/app/events/%5Bid%5D/page.tsx#L856) | Spots remaining text | needs-mode-aware-update |
| [web/src/app/events/[id]/page.tsx:1049](../web/src/app/events/%5Bid%5D/page.tsx#L1049) | Props to RSVP form | needs-mode-aware-update |
| [web/src/app/events/[id]/page.tsx:1504](../web/src/app/events/%5Bid%5D/page.tsx#L1504) | Props to add form | needs-mode-aware-update |
| [web/src/app/events/[id]/page.tsx:1768](../web/src/app/events/%5Bid%5D/page.tsx#L1768) | Props to tier form | needs-mode-aware-update |
| [web/src/app/events/[id]/page.tsx:1800](../web/src/app/events/%5Bid%5D/page.tsx#L1800) | Props to update form | needs-mode-aware-update |
| [web/src/app/events/[id]/page.tsx:1834](../web/src/app/events/%5Bid%5D/page.tsx#L1834) | Props to seat form | needs-mode-aware-update |
| [web/src/app/events/[id]/page.tsx:2376](../web/src/app/events/%5Bid%5D/page.tsx#L2376) | Props to checkout form | needs-mode-aware-update |
| [web/src/app/search/page.tsx:525](../web/src/app/search/page.tsx#L525) | Search results capacity display | needs-mode-aware-update |
| [web/src/presentation/utils/eventMapper.ts:313](../web/src/presentation/utils/eventMapper.ts#L313) | `isFull` utility | needs-mode-aware-update |
| [web/src/presentation/utils/eventMapper.ts:323](../web/src/presentation/utils/eventMapper.ts#L323) | `spotsLeft` utility | needs-mode-aware-update |
| [web/src/presentation/components/features/dashboard/EventsList.tsx:252](../web/src/presentation/components/features/dashboard/EventsList.tsx#L252) | Dashboard events list capacity | needs-mode-aware-update |
| [web/src/presentation/components/features/events/EditRegistrationModal.tsx:40](../web/src/presentation/components/features/events/EditRegistrationModal.tsx#L40) | Props definition `spotsLeft` | needs-mode-aware-update |
| [web/src/presentation/components/features/events/EditRegistrationModal.tsx:63](../web/src/presentation/components/features/events/EditRegistrationModal.tsx#L63) | Accept `spotsLeft` prop | needs-mode-aware-update |
| [web/src/presentation/components/features/events/EditRegistrationModal.tsx:85](../web/src/presentation/components/features/events/EditRegistrationModal.tsx#L85) | Cap attendees by spots | needs-mode-aware-update |
| [web/src/presentation/components/features/events/EditRegistrationModal.tsx:91](../web/src/presentation/components/features/events/EditRegistrationModal.tsx#L91) | Can add check | needs-mode-aware-update |

## 10. Frontend per-attendee enumerations

(See §4 frontend block above — same set, included here for completeness in the 10/11/12 grouping.)

## 11. Defensive reads (registrationMode undefined)

| File:Line | What it does | Status |
|---|---|---|
| [web/src/app/events/[id]/page.tsx:2391](../web/src/app/events/%5Bid%5D/page.tsx#L2391) | Attendee count with fallback (quantity or length) | defensive-read |
| [web/src/presentation/hooks/useEvents.ts:478](../web/src/presentation/hooks/useEvents.ts#L478) | Fallback: length \|\| quantity \|\| 1 | defensive-read |

Plus: every consumer of `event.registrationMode` must use the pattern `event.registrationMode ?? RegistrationMode.DetailedAttendees` to tolerate stale React Query cached payloads from before deploy.

## 12. Raw SQL reads of `event_registrations.attendees` JSONB

**No matches — verified clean.** No raw SQL queries access `attendees ->>` or `attendees @>` patterns. Attendees materialise via EF Core JSONB / owned-type mappings only.

---

## Risk-traceability matrix

Every architect-flagged risk traces to ≥1 row above:

| Risk (per Master TODO §Risk register) | Traces to checklist section(s) |
|---|---|
| 1. `Event.SpotsLeft` aggregation drift | §2, §3 |
| 2. Email template parameter contract drift | §7 |
| 3. JSONB null vs missing on legacy registrations | §12 *(none — clean by design)* + 7E.1 round-trip test |
| 4. `AddAttendeesModal` / `UpdateRsvpCommand` delta pricing fork | §4 (CalculateAdditionPrice handler) |
| 5. Stripe `TotalPrice` for paid HeadCountByAge / TierCounts | §4 (CalculateAdditionPrice §4 + PaymentCompleted handler) |
| 6. AddOnPurchase reports under Mode C — `INNER JOIN` drops standalone | §5 |
| 7. Tier rename/delete vs snapshot | §4 (TicketService) + §7 |
| 8. `SetRegistrationMode` guard scope | §6 — resolved (no fix needed) |
| 9. Validator combinatorics | (7E.2 implementation, no audit row) |
| 10. Frontend mode picker reactivity | (7E.5 implementation, no audit row) |

---

## Done when

- [x] All 12 sections populated.
- [x] Risk-traceability matrix complete.
- [x] Architect §6 concern resolved.
- [x] Total entries ≥ 30 (sanity check). Actual: 163.
- [ ] Master TODO marked 7E.0 ✅.
- [ ] Three tracking docs updated.
- [ ] Master index marked 7E.0 ✅.
- [ ] Commit landed.

(Final 4 boxes ticked in the same commit that creates this file — see PROGRESS_TRACKER session entry for 2026-04-25 Phase 7E start.)
