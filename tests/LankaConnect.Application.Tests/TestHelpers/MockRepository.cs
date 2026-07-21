using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Modules.Identity.Domain.Entities;
using LankaConnect.Modules.Identity.Domain.Repositories;
using Email = LankaConnect.SharedKernel.Contact.Email;

namespace LankaConnect.Application.Tests.TestHelpers;

// Wave 8.5.k (2026-07-16): CreateBusinessRepository removed alongside Businesses
// controller retirement per founder direction. Restore alongside LankaBusiness
// product re-add in Phase B.
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

    public static Mock<IUnitOfWork> CreateUnitOfWork()
    {
        var mock = new Mock<IUnitOfWork>();

        mock.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        return mock;
    }
}
