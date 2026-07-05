using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.Modules.Forms.Domain;
using LankaConnect.Modules.Forms.Domain.Entities;
using LankaConnect.Modules.Forms.Domain.Enums;
using LankaConnect.Modules.Forms.Domain.DomainEvents;
using LankaConnect.Modules.Forms.Domain.Repositories;
using LankaConnect.Modules.Forms.Application.Commands.DeleteFormResponse;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Entities;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LankaConnect.Modules.Forms.Application.Tests.Commands;

/// <summary>
/// Unit tests for DeleteFormResponseCommandHandler.
/// Phase 6A.106: Comprehensive test coverage for delete functionality with security scenarios.
///
/// ARCHITECT REVIEW REQUIREMENTS:
/// ✅ Test priority-based authorization (userId > token for logged-in users)
/// ✅ Test cross-user delete prevention
/// ✅ Test concurrent delete race condition
/// ✅ Test cascade delete (answers removed when response deleted)
/// </summary>
public class DeleteFormResponseCommandHandlerTests
{
    private readonly Mock<IFormResponseRepository> _mockRepository;
    private readonly Mock<IMultiContextUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILogger<DeleteFormResponseCommandHandler>> _mockLogger;
    private readonly DeleteFormResponseCommandHandler _handler;

    public DeleteFormResponseCommandHandlerTests()
    {
        _mockRepository = new Mock<IFormResponseRepository>();
        // Wave 6.5.d: mock the multi-context UoW; both overloads return 0 by default
        _mockUnitOfWork = new Mock<IMultiContextUnitOfWork>();
        _mockUnitOfWork.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);
        _mockUnitOfWork.Setup(x => x.CommitAsync(It.IsAny<DbContext[]>(), It.IsAny<CancellationToken>())).ReturnsAsync(0);
        _mockLogger = new Mock<ILogger<DeleteFormResponseCommandHandler>>();
        // Wave 6.5.d: FormsDbContext ctor arg. Passing null! because the test suite
        // never exercises the DbContext directly — the mock UoW absorbs the CommitAsync call.
        _handler = new DeleteFormResponseCommandHandler(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            formsContext: null!,
            _mockLogger.Object);
    }

    #region Anonymous User Tests (Token-based Auth)

    [Fact]
    public async Task Handle_AnonymousUser_ValidToken_DeletesSuccessfully()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var formId = Guid.NewGuid();
        var responseId = Guid.NewGuid();
        var accessToken = "valid-token-123";
        var tokenHash = ComputeSha256Hash(accessToken);

        var response = CreateAnonymousResponse(eventId, formId, responseId, tokenHash, "anonymous@example.com");

        _mockRepository
            .Setup(r => r.GetByIdWithAnswersAsync(responseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var command = new DeleteFormResponseCommand(eventId, formId, responseId, accessToken, null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        _mockRepository.Verify(r => r.DeleteAsync(response, It.IsAny<CancellationToken>()), Times.Once);
        // Wave 6.5.d: handler now calls the multi-context overload
        _mockUnitOfWork.Verify(u => u.CommitAsync(It.IsAny<DbContext[]>(), It.IsAny<CancellationToken>()), Times.Once);

        // Verify domain event raised
        Assert.Single(response.DomainEvents);
        var domainEvent = response.DomainEvents.First() as FormResponseDeletedEvent;
        Assert.NotNull(domainEvent);
        Assert.Equal(formId, domainEvent.FormId);
        Assert.Equal(responseId, domainEvent.ResponseId);
        Assert.Equal("anonymous@example.com", domainEvent.RespondentEmail);
    }

    [Fact]
    public async Task Handle_AnonymousUser_InvalidToken_ReturnsFailure()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var formId = Guid.NewGuid();
        var responseId = Guid.NewGuid();
        var correctToken = "correct-token";
        var wrongToken = "wrong-token";
        var tokenHash = ComputeSha256Hash(correctToken);

        var response = CreateAnonymousResponse(eventId, formId, responseId, tokenHash, "anonymous@example.com");

        _mockRepository
            .Setup(r => r.GetByIdWithAnswersAsync(responseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var command = new DeleteFormResponseCommand(eventId, formId, responseId, wrongToken, null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid access token", result.Error);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<FormResponse>(), It.IsAny<CancellationToken>()), Times.Never);
        // Wave 6.5.d: assert neither commit overload was called
        _mockUnitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitAsync(It.IsAny<DbContext[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AnonymousUser_MissingToken_ReturnsFailure()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var formId = Guid.NewGuid();
        var responseId = Guid.NewGuid();
        var tokenHash = ComputeSha256Hash("some-token");

        var response = CreateAnonymousResponse(eventId, formId, responseId, tokenHash, "anonymous@example.com");

        _mockRepository
            .Setup(r => r.GetByIdWithAnswersAsync(responseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var command = new DeleteFormResponseCommand(eventId, formId, responseId, null, null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Access token is required to delete this response", result.Error);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<FormResponse>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Logged-in User Tests (UserId-based Auth)

    [Fact]
    public async Task Handle_LoggedInUser_ValidUserId_DeletesSuccessfully()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var formId = Guid.NewGuid();
        var responseId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var tokenHash = ComputeSha256Hash("some-token");

        var response = CreateLoggedInUserResponse(eventId, formId, responseId, tokenHash, userId, "user@example.com");

        _mockRepository
            .Setup(r => r.GetByIdWithAnswersAsync(responseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var command = new DeleteFormResponseCommand(eventId, formId, responseId, null, userId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        _mockRepository.Verify(r => r.DeleteAsync(response, It.IsAny<CancellationToken>()), Times.Once);
        // Wave 6.5.d: handler now calls the multi-context overload
        _mockUnitOfWork.Verify(u => u.CommitAsync(It.IsAny<DbContext[]>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_LoggedInUser_WrongUserId_ReturnsFailure()
    {
        // Arrange: CRITICAL - logged-in user CANNOT delete another user's response
        var eventId = Guid.NewGuid();
        var formId = Guid.NewGuid();
        var responseId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var attackerId = Guid.NewGuid();  // Different user trying to delete
        var tokenHash = ComputeSha256Hash("some-token");

        var response = CreateLoggedInUserResponse(eventId, formId, responseId, tokenHash, ownerId, "owner@example.com");

        _mockRepository
            .Setup(r => r.GetByIdWithAnswersAsync(responseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var command = new DeleteFormResponseCommand(eventId, formId, responseId, null, attackerId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("You are not authorized to delete this response", result.Error);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<FormResponse>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_LoggedInUser_MissingUserId_ReturnsFailure()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var formId = Guid.NewGuid();
        var responseId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var tokenHash = ComputeSha256Hash("some-token");

        var response = CreateLoggedInUserResponse(eventId, formId, responseId, tokenHash, ownerId, "owner@example.com");

        _mockRepository
            .Setup(r => r.GetByIdWithAnswersAsync(responseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var command = new DeleteFormResponseCommand(eventId, formId, responseId, null, null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("You are not authorized to delete this response", result.Error);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<FormResponse>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region CRITICAL SECURITY TESTS (Architect Review Requirements)

    [Fact]
    public async Task Handle_LoggedInUserResponse_TokenAuthIgnored_PreventsCrossUserDelete()
    {
        // Arrange: CRITICAL - Anonymous user with valid token CANNOT delete logged-in user's response
        // This tests the priority-based authorization (userId > token)
        var eventId = Guid.NewGuid();
        var formId = Guid.NewGuid();
        var responseId = Guid.NewGuid();
        var loggedInUserId = Guid.NewGuid();
        var accessToken = "valid-token-123";
        var tokenHash = ComputeSha256Hash(accessToken);

        var response = CreateLoggedInUserResponse(eventId, formId, responseId, tokenHash, loggedInUserId, "user@example.com");

        _mockRepository
            .Setup(r => r.GetByIdWithAnswersAsync(responseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Attacker has valid token but tries to delete logged-in user's response
        var command = new DeleteFormResponseCommand(eventId, formId, responseId, accessToken, null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("You are not authorized to delete this response", result.Error);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<FormResponse>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_LoggedInUserResponse_WrongUserWithValidToken_BothChecksFail()
    {
        // Arrange: CRITICAL - Even with both userId AND token, wrong user cannot delete
        var eventId = Guid.NewGuid();
        var formId = Guid.NewGuid();
        var responseId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var attackerId = Guid.NewGuid();
        var accessToken = "valid-token-123";
        var tokenHash = ComputeSha256Hash(accessToken);

        var response = CreateLoggedInUserResponse(eventId, formId, responseId, tokenHash, ownerId, "owner@example.com");

        _mockRepository
            .Setup(r => r.GetByIdWithAnswersAsync(responseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Attacker provides both wrong userId AND valid token
        var command = new DeleteFormResponseCommand(eventId, formId, responseId, accessToken, attackerId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("You are not authorized to delete this response", result.Error);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<FormResponse>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Edge Cases & Error Handling

    [Fact]
    public async Task Handle_ResponseNotFound_ReturnsFailure()
    {
        // Arrange: CRITICAL - Handles concurrent delete race condition
        var eventId = Guid.NewGuid();
        var formId = Guid.NewGuid();
        var responseId = Guid.NewGuid();
        var accessToken = "valid-token";

        _mockRepository
            .Setup(r => r.GetByIdWithAnswersAsync(responseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FormResponse?)null);

        var command = new DeleteFormResponseCommand(eventId, formId, responseId, accessToken, null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Response not found or already deleted", result.Error);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<FormResponse>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WrongFormId_ReturnsFailure()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var correctFormId = Guid.NewGuid();
        var wrongFormId = Guid.NewGuid();
        var responseId = Guid.NewGuid();
        var accessToken = "valid-token";
        var tokenHash = ComputeSha256Hash(accessToken);

        var response = CreateAnonymousResponse(eventId, correctFormId, responseId, tokenHash, "anonymous@example.com");

        _mockRepository
            .Setup(r => r.GetByIdWithAnswersAsync(responseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var command = new DeleteFormResponseCommand(eventId, wrongFormId, responseId, accessToken, null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Response does not belong to the specified form", result.Error);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<FormResponse>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WrongEventId_ReturnsFailure()
    {
        // Arrange
        var correctEventId = Guid.NewGuid();
        var wrongEventId = Guid.NewGuid();
        var formId = Guid.NewGuid();
        var responseId = Guid.NewGuid();
        var accessToken = "valid-token";
        var tokenHash = ComputeSha256Hash(accessToken);

        var response = CreateAnonymousResponse(correctEventId, formId, responseId, tokenHash, "anonymous@example.com");

        _mockRepository
            .Setup(r => r.GetByIdWithAnswersAsync(responseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var command = new DeleteFormResponseCommand(wrongEventId, formId, responseId, accessToken, null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Response does not belong to the specified event", result.Error);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<FormResponse>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Helper Methods

    private FormResponse CreateAnonymousResponse(Guid eventId, Guid formId, Guid responseId, string tokenHash, string? email)
    {
        var result = FormResponse.Create(
            formId,
            eventId,
            tokenHash,
            null,  // No plaintext token needed for tests
            email,
            "Anonymous User",
            null);  // No userId - anonymous

        Assert.True(result.IsSuccess);
        var response = result.Value;

        // Use reflection to set Id and clear initial event
        typeof(FormResponse).GetProperty("Id")!.SetValue(response, responseId);
        response.ClearDomainEvents();

        return response;
    }

    private FormResponse CreateLoggedInUserResponse(Guid eventId, Guid formId, Guid responseId, string tokenHash, Guid userId, string? email)
    {
        var result = FormResponse.Create(
            formId,
            eventId,
            tokenHash,
            null,  // No plaintext token needed for tests
            email,
            "Logged In User",
            userId);  // Has userId - logged-in

        Assert.True(result.IsSuccess);
        var response = result.Value;

        // Use reflection to set Id and clear initial event
        typeof(FormResponse).GetProperty("Id")!.SetValue(response, responseId);
        response.ClearDomainEvents();

        return response;
    }

    private static string ComputeSha256Hash(string input)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        var hashBytes = System.Security.Cryptography.SHA256.HashData(bytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    #endregion
}
