// Global usings for LankaConnect.Application — Phase A transitional file.
// Mirrors the global aliases set in LankaConnect.Domain/Shared/NamespaceAliases.cs
// so Cultural types lifted to SharedKernel.Cultural resolve without per-file
// using-directive churn across ~31 Application consumers.
//
// W2C.2 (2026-06-04): SouthAsianLanguage moved to SharedKernel.Cultural per ADR-008.
// Subsequent Wave 2 sub-waves will add more cultural type aliases here as they
// migrate (W2C.3 ReligiousContext, W2C.4 CulturalBackground, etc.).

global using LankaConnect.SharedKernel.Cultural;
