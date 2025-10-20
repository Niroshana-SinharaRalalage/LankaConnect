using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Communications.Queries.GetEmailStatus;
using LankaConnect.Application.Communications.Common;
using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Communications.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using Xunit;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications.ValueObjects;

namespace LankaConnect.Application.Tests.Communications.Queries;

public class GetEmailStatusQueryHandlerTests
{
    private readonly Mock<IEmailStatusRepository> _emailStatusRepository;
    private readonly Mock<LankaConnect.Domain.Users.IUserRepository> _userRepository;
    private readonly Mock<ILogger<GetEmailStatusQueryHandler>> _logger;
    private readonly GetEmailStatusQueryHandler _handler;

    public GetEmailStatusQueryHandlerTests()
    {
        _emailStatusRepository = new Mock<IEmailStatusRepository>();
        _userRepository = new Mock<LankaConnect.Domain.Users.IUserRepository>();
        _logger = new Mock<ILogger<GetEmailStatusQueryHandler>>();
        _handler = new GetEmailStatusQueryHandler(_emailStatusRepository.Object, _userRepository.Object, _logger.Object);
    }

    [Fact]
    public async Task Handle_WithValidQuery_ShouldReturnEmailStatus()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var query = new GetEmailStatusQuery(userId);
        
        var user = CreateTestUser();
        var emailMessages = new List<LankaConnect.Domain.Communications.Entities.EmailMessage>
        {
            CreateTestEmailMessage("test@example.com", "Test Subject 1"),
            CreateTestEmailMessage("test2@example.com", "Test Subject 2")
        };

        _userRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
            
        _emailStatusRepository.Setup(x => x.GetEmailStatusAsync(
            userId,
            null,
            null,
            null,
            null,
            null,
            1,
            20,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(emailMessages);
            
        _emailStatusRepository.Setup(x => x.GetEmailStatusCountAsync(
            userId,
            null,
            null,
            null,
            null,
            null,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Emails.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(2);
        result.Value.PageNumber.Should().Be(1);
        result.Value.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task Handle_WithInvalidUserId_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var query = new GetEmailStatusQuery(userId);

        _userRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LankaConnect.Domain.Users.User?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("User not found");
    }

    [Fact]
    public async Task Handle_WithInvalidDateRange_ShouldReturnFailure()
    {
        // Arrange
        var fromDate = DateTime.UtcNow;
        var toDate = DateTime.UtcNow.AddDays(-1);
        var query = new GetEmailStatusQuery(FromDate: fromDate, ToDate: toDate);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("From date cannot be later than to date");
    }

    [Fact]
    public async Task Handle_WithInvalidPageNumber_ShouldReturnFailure()
    {
        // Arrange
        var query = new GetEmailStatusQuery(PageNumber: 0);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Page number must be greater than 0");
    }

    [Fact]
    public async Task Handle_WithInvalidPageSize_ShouldReturnFailure()
    {
        // Arrange
        var query = new GetEmailStatusQuery(PageSize: 0);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Page size must be between 1 and 100");
    }

    [Fact]
    public async Task Handle_WithPageSizeTooLarge_ShouldReturnFailure()
    {
        // Arrange
        var query = new GetEmailStatusQuery(PageSize: 101);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Page size must be between 1 and 100");
    }

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var query = new GetEmailStatusQuery(userId);
        var cancellationToken = new CancellationToken(true);

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _handler.Handle(query, cancellationToken));
    }

    [Fact]
    public async Task Handle_WithoutUserId_ShouldReturnResults()
    {
        // Arrange
        var query = new GetEmailStatusQuery();
        
        var emailMessages = new List<LankaConnect.Domain.Communications.Entities.EmailMessage>
        {
            CreateTestEmailMessage("test@example.com", "Test Subject")
        };

        _emailStatusRepository.Setup(x => x.GetEmailStatusAsync(
            null,
            null,
            null,
            null,
            null,
            null,
            1,
            20,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(emailMessages);
            
        _emailStatusRepository.Setup(x => x.GetEmailStatusCountAsync(
            null,
            null,
            null,
            null,
            null,
            null,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Emails.Should().HaveCount(1);
    }

    private static LankaConnect.Domain.Users.User CreateTestUser()
    {
        var email = LankaConnect.Domain.Shared.ValueObjects.Email.Create("test@example.com").Value;
        return LankaConnect.Domain.Users.User.Create(email, "Test", "User").Value;
    }

    private static LankaConnect.Domain.Communications.Entities.EmailMessage CreateTestEmailMessage(string toEmail, string subject)
    {
        var fromEmail = LankaConnect.Domain.Shared.ValueObjects.Email.Create("system@lankaconnect.com").Value;
        var emailSubject = EmailSubject.Create(subject).Value;
        var emailMessage = LankaConnect.Domain.Communications.Entities.EmailMessage.Create(
            fromEmail,
            emailSubject,
            "Test body",
            null,
            LankaConnect.Domain.Communications.Enums.EmailType.Transactional,
            3).Value;
        
        // Add the recipient
        var toEmailValue = LankaConnect.Domain.Shared.ValueObjects.Email.Create(toEmail).Value;
        emailMessage.AddRecipient(toEmailValue);
        
        return emailMessage;
    }
}