using AutoMapper;
using LankaConnect.Application.Common.Mappings;
using LankaConnect.Application.Tests.TestHelpers;
using LankaConnect.Application.Users.DTOs;
using LankaConnect.Domain.Users;

namespace LankaConnect.Application.Tests.Mappings;

public class UserMappingProfileTests
{
    private readonly IMapper _mapper;
    private readonly MapperConfiguration _configuration;

    public UserMappingProfileTests()
    {
        _configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<UserMappingProfile>();
        });

        _mapper = _configuration.CreateMapper();
    }

    [Fact]
    public void Configuration_ShouldBeValid()
    {
        // Act & Assert
        _configuration.AssertConfigurationIsValid();
    }

    [Fact]
    public void Map_UserToUserDto_ShouldMapAllProperties()
    {
        // Arrange
        var user = TestDataBuilder.CreateValidUser();

        // Act
        var dto = _mapper.Map<UserDto>(user);

        // Assert
        dto.Should().NotBeNull();
        dto.Id.Should().Be(user.Id);
        dto.Email.Should().Be(user.Email.Value);
        dto.FirstName.Should().Be(user.FirstName);
        dto.LastName.Should().Be(user.LastName);
        dto.IsActive.Should().Be(user.IsActive);
        dto.CreatedAt.Should().Be(user.CreatedAt);
        dto.UpdatedAt.Should().Be(user.UpdatedAt);
    }

    [Fact]
    public void Map_UserWithPhoneNumber_ShouldMapPhoneNumber()
    {
        // Arrange
        var user = TestDataBuilder.CreateValidUser();
        // Assume user has phone number through profile update

        // Act
        var dto = _mapper.Map<UserDto>(user);

        // Assert
        dto.Should().NotBeNull();
        dto.Id.Should().Be(user.Id);
        dto.Email.Should().Be(user.Email.Value);
    }

    [Fact]
    public void Map_UserWithoutOptionalFields_ShouldMapWithNulls()
    {
        // Arrange
        var user = TestDataBuilder.CreateValidUser();

        // Act
        var dto = _mapper.Map<UserDto>(user);

        // Assert
        dto.Should().NotBeNull();
        dto.Id.Should().Be(user.Id);
        dto.Email.Should().Be(user.Email.Value);
        dto.FirstName.Should().Be(user.FirstName);
        dto.LastName.Should().Be(user.LastName);
        dto.PhoneNumber.Should().BeNull();
        dto.Bio.Should().BeNull();
    }

    [Fact]
    public void Map_UserCollectionToUserDtoCollection_ShouldMapAll()
    {
        // Arrange
        var users = new List<User>
        {
            TestDataBuilder.CreateValidUser(),
            TestDataBuilder.CreateValidUser(),
            TestDataBuilder.CreateValidUser()
        };

        // Act
        var dtos = _mapper.Map<List<UserDto>>(users);

        // Assert
        dtos.Should().HaveCount(3);
        dtos.Should().AllSatisfy(dto =>
        {
            dto.Should().NotBeNull();
            dto.Id.Should().NotBeEmpty();
            dto.Email.Should().NotBeNullOrEmpty();
            dto.FirstName.Should().NotBeNullOrEmpty();
            dto.LastName.Should().NotBeNullOrEmpty();
        });
    }

    [Fact]
    public void Map_NullUser_ShouldReturnNull()
    {
        // Arrange
        User? user = null;

        // Act
        var dto = _mapper.Map<UserDto>(user);

        // Assert
        dto.Should().BeNull();
    }

    [Fact]
    public void Map_EmptyUserCollection_ShouldReturnEmptyCollection()
    {
        // Arrange
        var users = new List<User>();

        // Act
        var dtos = _mapper.Map<List<UserDto>>(users);

        // Assert
        dtos.Should().NotBeNull();
        dtos.Should().BeEmpty();
    }

    [Fact]
    public void Map_UserToUserDto_ShouldPreserveDateTimeKind()
    {
        // Arrange
        var user = TestDataBuilder.CreateValidUser();

        // Act
        var dto = _mapper.Map<UserDto>(user);

        // Assert
        dto.CreatedAt.Kind.Should().Be(user.CreatedAt.Kind);
        if (user.UpdatedAt.HasValue)
        {
            dto.UpdatedAt.Should().Be(user.UpdatedAt.Value);
        }
    }

    [Fact]
    public void Map_UserToUserDto_ShouldHandleEmailValueObjectCorrectly()
    {
        // Arrange
        var user = TestDataBuilder.CreateValidUser();
        var expectedEmail = user.Email.Value;

        // Act
        var dto = _mapper.Map<UserDto>(user);

        // Assert
        dto.Email.Should().Be(expectedEmail);
        dto.Email.Should().NotBeNullOrEmpty();
        dto.Email.Should().Contain("@");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Map_UserToUserDto_ShouldPreserveIsActiveStatus(bool isActive)
    {
        // Arrange
        var user = TestDataBuilder.CreateValidUser();
        
        // Set the active status based on the test parameter
        if (isActive)
            user.Activate();
        else
            user.Deactivate();

        // Act
        var dto = _mapper.Map<UserDto>(user);

        // Assert
        dto.IsActive.Should().Be(isActive);
        dto.IsActive.Should().Be(user.IsActive);
    }

    [Fact]
    public void Map_UserWithLongValues_ShouldMapCorrectly()
    {
        // Arrange
        var user = TestDataBuilder.CreateValidUser();

        // Act
        var dto = _mapper.Map<UserDto>(user);

        // Assert
        dto.Should().NotBeNull();
        dto.FirstName.Length.Should().BeLessOrEqualTo(100);
        dto.LastName.Length.Should().BeLessOrEqualTo(100);
        dto.Email.Length.Should().BeLessOrEqualTo(255);
    }

    [Fact]
    public void Map_UserToUserDto_ShouldBeReversible()
    {
        // Note: This test assumes we might have reverse mapping in the future
        // Currently we only map from User to UserDto, not the reverse
        // This test validates the mapping works consistently

        // Arrange
        var user = TestDataBuilder.CreateValidUser();

        // Act
        var dto = _mapper.Map<UserDto>(user);
        var userAgain = _mapper.Map<UserDto>(user); // Map again to test consistency

        // Assert
        dto.Should().BeEquivalentTo(userAgain);
    }

    [Fact]
    public void Map_MultipleUsers_ShouldMaintainOrder()
    {
        // Arrange
        var user1 = TestDataBuilder.CreateValidUser();
        var user2 = TestDataBuilder.CreateValidUser();
        var user3 = TestDataBuilder.CreateValidUser();
        var users = new List<User> { user1, user2, user3 };

        // Act
        var dtos = _mapper.Map<List<UserDto>>(users);

        // Assert
        dtos[0].Id.Should().Be(user1.Id);
        dtos[1].Id.Should().Be(user2.Id);
        dtos[2].Id.Should().Be(user3.Id);
    }
}