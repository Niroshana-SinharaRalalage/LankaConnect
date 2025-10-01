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
global using LankaConnect.Domain.Communications.Entities;
global using LankaConnect.Domain.Communications.ValueObjects;
global using LankaConnect.Domain.Communications.Enums;
global using LankaConnect.Domain.Shared.ValueObjects;
global using LankaConnect.Domain.Shared.Enums;
global using LankaConnect.Domain.Users.ValueObjects;
global using LankaConnect.Domain.Common;

// Application interfaces
global using LankaConnect.Application.Common.Interfaces;

// Test utilities
global using LankaConnect.TestUtilities.Builders;