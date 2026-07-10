// Global usings for TestUtilities project
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

// Domain
global using LankaConnect.Domain.Business;
global using LankaConnect.Domain.Business.ValueObjects;
global using LankaConnect.Domain.Business.Enums;
global using LankaConnect.Modules.Communications.Domain.Entities;
global using LankaConnect.Modules.Communications.Domain.ValueObjects;
global using LankaConnect.Modules.Communications.Domain.Enums;
global using LankaConnect.BuildingBlocks.Domain.Shared.ValueObjects;
global using LankaConnect.BuildingBlocks.Domain.Shared.Enums;
global using LankaConnect.Modules.Identity.Domain.ValueObjects;
global using LankaConnect.Domain.Common;

// Application interfaces
global using LankaConnect.BuildingBlocks.Application.Common.Interfaces;

// Test utilities
global using LankaConnect.TestUtilities.Builders;