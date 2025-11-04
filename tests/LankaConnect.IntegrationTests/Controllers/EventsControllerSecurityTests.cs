using Xunit;
using Moq;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using LankaConnect.API.Controllers;
using LankaConnect.API.Extensions;
using LankaConnect.Application.Events.Commands.AddToWaitingList;
using LankaConnect.Application.Events.Commands.RemoveFromWaitingList;
using LankaConnect.Application.Events.Commands.PromoteFromWaitingList;
using LankaConnect.Application.Events.Commands.CancelRsvp;
using LankaConnect.Application.Events.Queries.GetUserRsvps;
using LankaConnect.Application.Events.Queries.GetUpcomingEventsForUser;
using LankaConnect.Application.Common.Models;
using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Common;

namespace LankaConnect.IntegrationTests.Controllers;

/// <summary>
/// Security tests for authentication parameter vulnerability (ADR-002)
/// Tests that endpoints extract userId from JWT claims, not query parameters
///
/// CRITICAL: These tests verify fix for OWASP A01:2021 - Broken Access Control
/// Users must not be able to impersonate other users via query parameters
/// </summary>
public class EventsControllerSecurityTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ILogger<EventsController>> _loggerMock;
    private readonly EventsController _controller;
    private readonly Guid _authenticatedUserId;

    public EventsControllerSecurityTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _loggerMock = new Mock<ILogger<EventsController>>();
        _authenticatedUserId = Guid.NewGuid();

        // Setup controller with authenticated user context
        _controller = new EventsController(_mediatorMock.Object, _loggerMock.Object);
        SetupAuthenticatedUser(_controller, _authenticatedUserId);
    }

    #region Waiting List Endpoints (Epic 2)

    [Fact]
    public async Task AddToWaitingList_Should_Use_JWT_UserId_Not_Query_Parameter()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        _mediatorMock.Setup(m => m.Send(It.IsAny<AddToWaitingListCommand>(), default))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _controller.AddToWaitingList(eventId);

        // Assert
        _mediatorMock.Verify(m => m.Send(
            It.Is<AddToWaitingListCommand>(cmd =>
                cmd.EventId == eventId &&
                cmd.UserId == _authenticatedUserId), // Must use JWT user ID
            default),
            Times.Once,
            "AddToWaitingList must extract userId from JWT token, not accept as parameter");

        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task RemoveFromWaitingList_Should_Use_JWT_UserId_Not_Query_Parameter()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        _mediatorMock.Setup(m => m.Send(It.IsAny<RemoveFromWaitingListCommand>(), default))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _controller.RemoveFromWaitingList(eventId);

        // Assert
        _mediatorMock.Verify(m => m.Send(
            It.Is<RemoveFromWaitingListCommand>(cmd =>
                cmd.EventId == eventId &&
                cmd.UserId == _authenticatedUserId),
            default),
            Times.Once,
            "RemoveFromWaitingList must extract userId from JWT token");

        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task PromoteFromWaitingList_Should_Use_JWT_UserId_Not_Query_Parameter()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        _mediatorMock.Setup(m => m.Send(It.IsAny<PromoteFromWaitingListCommand>(), default))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _controller.PromoteFromWaitingList(eventId);

        // Assert
        _mediatorMock.Verify(m => m.Send(
            It.Is<PromoteFromWaitingListCommand>(cmd =>
                cmd.EventId == eventId &&
                cmd.UserId == _authenticatedUserId),
            default),
            Times.Once,
            "PromoteFromWaitingList must extract userId from JWT token");

        Assert.IsType<OkResult>(result);
    }

    #endregion

    #region RSVP Endpoints (Existing Vulnerability)

    [Fact]
    public async Task CancelRsvp_Should_Use_JWT_UserId_Not_Query_Parameter()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        _mediatorMock.Setup(m => m.Send(It.IsAny<CancelRsvpCommand>(), default))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _controller.CancelRsvp(eventId);

        // Assert
        _mediatorMock.Verify(m => m.Send(
            It.Is<CancelRsvpCommand>(cmd =>
                cmd.EventId == eventId &&
                cmd.UserId == _authenticatedUserId),
            default),
            Times.Once,
            "CancelRsvp must extract userId from JWT token");

        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task GetMyRsvps_Should_Use_JWT_UserId_Not_Query_Parameter()
    {
        // Arrange
        var expectedRsvps = new List<RsvpDto>();
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetUserRsvpsQuery>(), default))
            .ReturnsAsync(Result<IReadOnlyList<RsvpDto>>.Success(expectedRsvps));

        // Act
        var result = await _controller.GetMyRsvps();

        // Assert
        _mediatorMock.Verify(m => m.Send(
            It.Is<GetUserRsvpsQuery>(query =>
                query.UserId == _authenticatedUserId),
            default),
            Times.Once,
            "GetMyRsvps must extract userId from JWT token");

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetUpcomingEvents_Should_Use_JWT_UserId_Not_Query_Parameter()
    {
        // Arrange
        var expectedEvents = new List<EventDto>();
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetUpcomingEventsForUserQuery>(), default))
            .ReturnsAsync(Result<IReadOnlyList<EventDto>>.Success(expectedEvents));

        // Act
        var result = await _controller.GetUpcomingEvents();

        // Assert
        _mediatorMock.Verify(m => m.Send(
            It.Is<GetUpcomingEventsForUserQuery>(query =>
                query.UserId == _authenticatedUserId),
            default),
            Times.Once,
            "GetUpcomingEvents must extract userId from JWT token");

        Assert.IsType<OkObjectResult>(result);
    }

    #endregion

    #region Security Boundary Tests (Method Signature Validation)

    [Fact]
    public void AddToWaitingList_Should_Not_Have_UserId_Query_Parameter()
    {
        // Arrange
        var method = typeof(EventsController).GetMethod(nameof(EventsController.AddToWaitingList));

        // Assert
        var parameters = method!.GetParameters();
        Assert.Single(parameters); // Only eventId parameter
        Assert.Equal("id", parameters[0].Name);
        Assert.Equal(typeof(Guid), parameters[0].ParameterType);
        Assert.DoesNotContain(parameters, p => p.Name == "userId");
    }

    [Theory]
    [InlineData(nameof(EventsController.RemoveFromWaitingList))]
    [InlineData(nameof(EventsController.PromoteFromWaitingList))]
    [InlineData(nameof(EventsController.CancelRsvp))]
    public void WaitingList_And_RSVP_Methods_Should_Not_Accept_UserId_Parameter(string methodName)
    {
        // Arrange
        var method = typeof(EventsController).GetMethod(methodName);

        // Assert
        var parameters = method!.GetParameters();
        Assert.DoesNotContain(parameters, p => p.Name == "userId");
    }

    [Theory]
    [InlineData(nameof(EventsController.GetMyRsvps))]
    [InlineData(nameof(EventsController.GetUpcomingEvents))]
    public void User_Query_Methods_Should_Not_Accept_UserId_Parameter(string methodName)
    {
        // Arrange
        var method = typeof(EventsController).GetMethod(methodName);

        // Assert
        var parameters = method!.GetParameters();
        Assert.Empty(parameters); // No parameters at all
    }

    [Fact]
    public void AddToWaitingList_Should_Require_Authorization()
    {
        // Arrange
        var method = typeof(EventsController).GetMethod(nameof(EventsController.AddToWaitingList));

        // Assert
        var authorizeAttribute = method!.GetCustomAttributes(typeof(AuthorizeAttribute), false);
        Assert.NotEmpty(authorizeAttribute);
    }

    #endregion

    #region Helper Methods

    private static void SetupAuthenticatedUser(ControllerBase controller, Guid userId)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, "test@example.com")
        };

        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = claimsPrincipal
            }
        };
    }

    #endregion
}
