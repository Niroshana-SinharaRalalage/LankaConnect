using LankaConnect.Application.Businesses.Commands.DeleteBusiness;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Tests.TestHelpers;
using LankaConnect.Domain.Business;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Exceptions;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Tests.Businesses.Commands;

public class DeleteBusinessCommandHandlerTests
{
    private readonly Mock<IBusinessRepository> _mockBusinessRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILogger<DeleteBusinessCommandHandler>> _mockLogger;
    private readonly DeleteBusinessCommandHandler _handler;
    private readonly Fixture _fixture;

    public DeleteBusinessCommandHandlerTests()
    {
        _mockBusinessRepository = new Mock<IBusinessRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockLogger = new Mock<ILogger<DeleteBusinessCommandHandler>>();
        _handler = new DeleteBusinessCommandHandler(_mockBusinessRepository.Object, _mockUnitOfWork.Object, _mockLogger.Object);
        _fixture = new Fixture();
    }

    [Fact]
    public async Task Handle_WithValidBusinessId_ShouldDeleteBusinessSuccessfully()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var existingBusiness = TestDataBuilder.CreateValidBusiness(ownerId);
        var command = new DeleteBusinessCommand(businessId);

        _mockBusinessRepository
            .Setup(x => x.GetByIdAsync(businessId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingBusiness);

        _mockBusinessRepository
            .Setup(x => x.DeleteAsync(businessId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        _mockBusinessRepository.Verify(x => x.DeleteAsync(businessId, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithUsBusinessData_ShouldDeleteSuccessfully()
    {
        // Arrange - Test deletion of a US-based Sri Lankan business
        var businessId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var usBusiness = TestDataBuilder.CreateValidBusiness(ownerId);
        var command = new DeleteBusinessCommand(businessId);

        _mockBusinessRepository
            .Setup(x => x.GetByIdAsync(businessId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usBusiness);

        _mockBusinessRepository
            .Setup(x => x.DeleteAsync(businessId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mockBusinessRepository.Verify(x => x.GetByIdAsync(businessId, It.IsAny<CancellationToken>()), Times.Once);
        _mockBusinessRepository.Verify(x => x.DeleteAsync(businessId, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenBusinessNotFound_ShouldThrowBusinessNotFoundException()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var command = new DeleteBusinessCommand(businessId);

        _mockBusinessRepository
            .Setup(x => x.GetByIdAsync(businessId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Business?)null);

        // Act & Assert
        await _handler.Invoking(x => x.Handle(command, CancellationToken.None))
                     .Should().ThrowAsync<BusinessNotFoundException>()
                     .WithMessage($"Business with ID '{businessId}' was not found.");

        _mockBusinessRepository.Verify(x => x.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenRepositoryThrowsException_ShouldPropagateException()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var existingBusiness = TestDataBuilder.CreateValidBusiness(ownerId);
        var command = new DeleteBusinessCommand(businessId);

        _mockBusinessRepository
            .Setup(x => x.GetByIdAsync(businessId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingBusiness);

        _mockBusinessRepository
            .Setup(x => x.DeleteAsync(businessId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        await _handler.Invoking(x => x.Handle(command, CancellationToken.None))
                     .Should().ThrowAsync<InvalidOperationException>()
                     .WithMessage("Database error");

        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUnitOfWorkFails_ShouldPropagateException()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var existingBusiness = TestDataBuilder.CreateValidBusiness(ownerId);
        var command = new DeleteBusinessCommand(businessId);

        _mockBusinessRepository
            .Setup(x => x.GetByIdAsync(businessId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingBusiness);

        _mockBusinessRepository
            .Setup(x => x.DeleteAsync(businessId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Transaction failed"));

        // Act & Assert
        await _handler.Invoking(x => x.Handle(command, CancellationToken.None))
                     .Should().ThrowAsync<InvalidOperationException>()
                     .WithMessage("Transaction failed");
    }

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassTokenToRepository()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var existingBusiness = TestDataBuilder.CreateValidBusiness(ownerId);
        var command = new DeleteBusinessCommand(businessId);
        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        _mockBusinessRepository
            .Setup(x => x.GetByIdAsync(businessId, cancellationToken))
            .ReturnsAsync(existingBusiness);

        _mockBusinessRepository
            .Setup(x => x.DeleteAsync(businessId, cancellationToken))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork
            .Setup(x => x.CommitAsync(cancellationToken))
            .ReturnsAsync(1);

        // Act
        await _handler.Handle(command, cancellationToken);

        // Assert
        _mockBusinessRepository.Verify(x => x.GetByIdAsync(businessId, cancellationToken), Times.Once);
        _mockBusinessRepository.Verify(x => x.DeleteAsync(businessId, cancellationToken), Times.Once);
        _mockUnitOfWork.Verify(x => x.CommitAsync(cancellationToken), Times.Once);
    }

    [Fact]
    public async Task Handle_WithEmptyGuid_ShouldThrowBusinessNotFoundException()
    {
        // Arrange
        var command = new DeleteBusinessCommand(Guid.Empty);

        _mockBusinessRepository
            .Setup(x => x.GetByIdAsync(Guid.Empty, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Business?)null);

        // Act & Assert
        await _handler.Invoking(x => x.Handle(command, CancellationToken.None))
                     .Should().ThrowAsync<BusinessNotFoundException>()
                     .WithMessage($"Business with ID '{Guid.Empty}' was not found.");
        
        // Verify repository was called but no delete operations
        _mockBusinessRepository.Verify(x => x.GetByIdAsync(Guid.Empty, It.IsAny<CancellationToken>()), Times.Once);
        _mockBusinessRepository.Verify(x => x.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("d290f1ee-6c54-4b01-90e6-d701748f0851")]
    [InlineData("123e4567-e89b-12d3-a456-426614174000")]
    public async Task Handle_WithValidGuids_ShouldProcessCorrectly(string guidString)
    {
        // Arrange
        var businessId = Guid.Parse(guidString);
        var ownerId = Guid.NewGuid();
        var existingBusiness = TestDataBuilder.CreateValidBusiness(ownerId);
        var command = new DeleteBusinessCommand(businessId);

        _mockBusinessRepository
            .Setup(x => x.GetByIdAsync(businessId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingBusiness);

        _mockBusinessRepository
            .Setup(x => x.DeleteAsync(businessId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mockBusinessRepository.Verify(x => x.GetByIdAsync(businessId, It.IsAny<CancellationToken>()), Times.Once);
    }
}