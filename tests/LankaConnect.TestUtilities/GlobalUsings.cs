// Global usings for TestUtilities project
// Wave 8.5.e (2026-07-18): pruned stale namespaces after Consult #12 Option D
// (Business aggregate deleted), Consult #15 PASS C (Shared bucket deleted),
// and 4C.d/e/f/g (Domain.Common retired). Remaining usings track live namespaces only.
global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Threading;
global using System.Threading.Tasks;
global using System.IO;

// FluentAssertions
global using FluentAssertions;
global using FluentAssertions.Execution;

// Moq
global using Moq;

// AutoFixture
global using AutoFixture;

// Communications module — hosts EmailMessage, EmailSubject, Email VO, EmailType.
global using LankaConnect.Modules.Communications.Domain.Entities;
global using LankaConnect.Modules.Communications.Domain.ValueObjects;
global using LankaConnect.Modules.Communications.Domain.Enums;

// Identity module — hosts UserRole and related VOs.
global using LankaConnect.Modules.Identity.Domain.ValueObjects;

// Application interfaces
global using LankaConnect.BuildingBlocks.Application.Common.Interfaces;

// Test utilities
global using LankaConnect.TestUtilities.Builders;
