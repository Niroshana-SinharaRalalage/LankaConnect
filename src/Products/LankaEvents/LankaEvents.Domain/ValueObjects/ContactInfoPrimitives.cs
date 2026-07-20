using LankaConnect.BuildingBlocks.Domain;

namespace LankaConnect.Products.LankaEvents.Domain.ValueObjects;

// Wave 8.5-cleanup (2026-07-18) ExtractabilityAudit GAP-6 progress:
//   - Address + GeoCoordinate promoted to LankaConnect.SharedKernel.Geo per ADR-006
//     (see SharedKernel/SharedKernel.Geo/Address.cs + GeoCoordinate.cs).
//   - Email + PhoneNumber promoted to LankaConnect.SharedKernel.Contact per ADR-006
//     (see SharedKernel/SharedKernel.Contact/Email.cs + PhoneNumber.cs). Closes the
//     GAP-6 core inversion — Identity.User.Email + Identity.User.PhoneNumber no
//     longer type-depend on a Product namespace.
//   - CulturalAppropriateness promoted to LankaConnect.Modules.CulturalIntelligence.Contracts.Services
//     (Wave 8.5 Tech Lead D-13 Option A, 2026-07-19, GAP-1 Part 0) alongside
//     ICulturalCalendar interface + supporting DTOs. See
//     src/Modules/CulturalIntelligence/CulturalIntelligence.Contracts/Services/CulturalCalendarTypes.cs.
//
// Sprint Day 5 (2026-07-06) Consult #12 fallout note (preserved for history):
// the five VOs previously lived in the wiped LankaConnect.BuildingBlocks.Domain.Shared
// namespace; they were restored here as minimal stubs so LankaEvents.Domain compiles.
// This file drains as the promotions land.

// Email + PhoneNumber promoted to LankaConnect.SharedKernel.Contact
// (Wave 8.5-cleanup 2026-07-18, ExtractabilityAudit GAP-6).
// See src/SharedKernel/SharedKernel.Contact/Email.cs and PhoneNumber.cs.

// Address + GeoCoordinate promoted to LankaConnect.SharedKernel.Geo
// (Wave 8.5-cleanup 2026-07-18, ExtractabilityAudit GAP-6).
// See src/SharedKernel/SharedKernel.Geo/Address.cs and GeoCoordinate.cs.

// CulturalAppropriateness promoted to LankaConnect.Modules.CulturalIntelligence.Contracts.Services
// (Wave 8.5 Tech Lead D-13 Option A, 2026-07-19, GAP-1 Part 0).
// See src/Modules/CulturalIntelligence/CulturalIntelligence.Contracts/Services/CulturalCalendarTypes.cs.
