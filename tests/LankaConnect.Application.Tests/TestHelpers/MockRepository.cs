using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users;
using LankaConnect.Domain.Users.ValueObjects;
using LankaConnect.Domain.Business;
using Email = LankaConnect.Domain.Shared.ValueObjects.Email;

namespace LankaConnect.Application.Tests.TestHelpers;

public static class MockRepository
{
    public static Mock<IUserRepository> CreateUserRepository()
    {
        var mock = new Mock<IUserRepository>();
        
        // Default setup for common operations
        mock.Setup(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
            
        mock.Setup(x => x.ExistsWithEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        mock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        return mock;
    }

    public static Mock<IBusinessRepository> CreateBusinessRepository()
    {
        var mock = new Mock<IBusinessRepository>();
        
        mock.Setup(x => x.AddAsync(It.IsAny<Business>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
            
        mock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Business?)null);

        // Update method is void, not async Task
        mock.Setup(x => x.Update(It.IsAny<Business>()));

        mock.Setup(x => x.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return mock;
    }

    public static Mock<IUnitOfWork> CreateUnitOfWork()
    {
        var mock = new Mock<IUnitOfWork>();
        
        mock.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        return mock;
    }
}