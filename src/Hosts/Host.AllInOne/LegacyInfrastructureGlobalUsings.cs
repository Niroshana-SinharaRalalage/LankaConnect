// Global usings for LankaConnect.Infrastructure — Phase A transitional file.
// Mirrors aliases set in LankaConnect.Domain/Shared/NamespaceAliases.cs +
// LankaConnect.Application/GlobalUsings.cs so cultural types lifted to
// SharedKernel.Cultural resolve without per-file using-directive churn.
//
// W2C.5 (2026-06-05): GeographicRegion moved to SharedKernel.Cultural per
// ADR-008. Several Infrastructure files reference it unqualified via the
// (now-deleted) using LankaConnect.BuildingBlocks.Domain.Enums directive; the global
// using here keeps them compiling.

global using LankaConnect.SharedKernel.Cultural;
