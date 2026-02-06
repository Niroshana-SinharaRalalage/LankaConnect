using FluentAssertions;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications.Entities;
using Xunit;

namespace LankaConnect.Application.Tests.Domain.Communications;

/// <summary>
/// TDD Tests for EmailGroup Entity
/// Phase 6A.25: Email Groups Management
/// </summary>
public class EmailGroupTests
{
    private readonly Guid _ownerId = Guid.NewGuid();

    #region Create Tests

    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        // Arrange
        var name = "Marketing Team";
        var emailAddresses = "john@example.com, jane@example.com";
        var description = "Team for marketing communications";

        // Act
        var result = EmailGroup.Create(name, _ownerId, emailAddresses, description);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Marketing Team");
        result.Value.OwnerId.Should().Be(_ownerId);
        result.Value.Description.Should().Be("Team for marketing communications");
        result.Value.IsActive.Should().BeTrue();
        result.Value.GetEmailList().Should().HaveCount(2);
    }

    [Fact]
    public void Create_WithEmptyName_ShouldFail()
    {
        // Arrange
        var name = "";
        var emailAddresses = "john@example.com";

        // Act
        var result = EmailGroup.Create(name, _ownerId, emailAddresses);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Name is required");
    }

    [Fact]
    public void Create_WithWhitespaceName_ShouldFail()
    {
        // Arrange
        var name = "   ";
        var emailAddresses = "john@example.com";

        // Act
        var result = EmailGroup.Create(name, _ownerId, emailAddresses);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Name is required");
    }

    [Fact]
    public void Create_WithEmptyEmails_ShouldFail()
    {
        // Arrange
        var name = "Test Group";
        var emailAddresses = "";

        // Act
        var result = EmailGroup.Create(name, _ownerId, emailAddresses);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("At least one email address is required");
    }

    [Fact]
    public void Create_WithNullDescription_ShouldSucceed()
    {
        // Arrange
        var name = "Test Group";
        var emailAddresses = "john@example.com";

        // Act
        var result = EmailGroup.Create(name, _ownerId, emailAddresses, null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Description.Should().BeNull();
    }

    #endregion

    #region Email Validation Tests

    [Fact]
    public void Create_WithInvalidEmail_ShouldFail()
    {
        // Arrange
        var name = "Test Group";
        var emailAddresses = "invalid-email";

        // Act
        var result = EmailGroup.Create(name, _ownerId, emailAddresses);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Invalid email format");
    }

    [Fact]
    public void Create_WithMixedValidAndInvalidEmails_ShouldFail()
    {
        // Arrange
        var name = "Test Group";
        var emailAddresses = "john@example.com, invalid-email, jane@test.org";

        // Act
        var result = EmailGroup.Create(name, _ownerId, emailAddresses);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Invalid email format");
        result.Error.Should().Contain("invalid-email");
    }

    [Fact]
    public void Create_WithDuplicateEmails_ShouldDeduplicate()
    {
        // Arrange
        var name = "Test Group";
        var emailAddresses = "john@example.com, john@example.com, jane@example.com";

        // Act
        var result = EmailGroup.Create(name, _ownerId, emailAddresses);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.GetEmailList().Should().HaveCount(2);
        result.Value.GetEmailList().Should().Contain("john@example.com");
        result.Value.GetEmailList().Should().Contain("jane@example.com");
    }

    [Fact]
    public void Create_ShouldNormalizeEmailsToLowerCase()
    {
        // Arrange
        var name = "Test Group";
        var emailAddresses = "John@Example.COM, JANE@TEST.ORG";

        // Act
        var result = EmailGroup.Create(name, _ownerId, emailAddresses);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.GetEmailList().Should().Contain("john@example.com");
        result.Value.GetEmailList().Should().Contain("jane@test.org");
    }

    [Fact]
    public void Create_WithExtraWhitespace_ShouldTrim()
    {
        // Arrange
        var name = "  Test Group  ";
        var emailAddresses = "  john@example.com  ,  jane@example.com  ";

        // Act
        var result = EmailGroup.Create(name, _ownerId, emailAddresses);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Test Group");
        result.Value.GetEmailList().Should().Contain("john@example.com");
    }

    [Fact]
    public void Create_WithOnlyCommasAndWhitespace_ShouldFail()
    {
        // Arrange
        var name = "Test Group";
        var emailAddresses = ",  ,  ,";

        // Act
        var result = EmailGroup.Create(name, _ownerId, emailAddresses);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("At least one valid email address is required");
    }

    [Theory]
    [InlineData("test@example.com")]
    [InlineData("user.name@domain.org")]
    [InlineData("user+tag@example.co.uk")]
    [InlineData("user_name123@sub.domain.com")]
    public void Create_WithValidEmailFormats_ShouldSucceed(string email)
    {
        // Arrange
        var name = "Test Group";

        // Act
        var result = EmailGroup.Create(name, _ownerId, email);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("@domain.com")]
    [InlineData("user@")]
    [InlineData("user@domain")]
    [InlineData("user domain@test.com")]
    public void Create_WithInvalidEmailFormats_ShouldFail(string email)
    {
        // Arrange
        var name = "Test Group";

        // Act
        var result = EmailGroup.Create(name, _ownerId, email);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Invalid email format");
    }

    #endregion

    #region Update Tests

    [Fact]
    public void Update_WithValidData_ShouldSucceed()
    {
        // Arrange
        var group = EmailGroup.Create("Original Name", _ownerId, "john@example.com").Value;
        var newName = "Updated Name";
        var newEmails = "jane@example.com, bob@test.org";
        var newDescription = "Updated description";

        // Act
        var result = group.Update(newName, newEmails, newDescription);

        // Assert
        result.IsSuccess.Should().BeTrue();
        group.Name.Should().Be("Updated Name");
        group.Description.Should().Be("Updated description");
        group.GetEmailList().Should().HaveCount(2);
        group.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Update_WithEmptyName_ShouldFail()
    {
        // Arrange
        var group = EmailGroup.Create("Original Name", _ownerId, "john@example.com").Value;

        // Act
        var result = group.Update("", "jane@example.com", null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Name is required");
        group.Name.Should().Be("Original Name"); // Should not change
    }

    [Fact]
    public void Update_WithInvalidEmails_ShouldFail()
    {
        // Arrange
        var group = EmailGroup.Create("Original Name", _ownerId, "john@example.com").Value;

        // Act
        var result = group.Update("New Name", "invalid-email", null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Invalid email format");
        group.GetEmailList().Should().Contain("john@example.com"); // Should not change
    }

    #endregion

    #region Deactivate Tests

    [Fact]
    public void Deactivate_ShouldSetIsActiveToFalse()
    {
        // Arrange
        var group = EmailGroup.Create("Test Group", _ownerId, "john@example.com").Value;

        // Act
        group.Deactivate();

        // Assert
        group.IsActive.Should().BeFalse();
        group.UpdatedAt.Should().NotBeNull();
    }

    #endregion

    #region GetEmailList Tests

    [Fact]
    public void GetEmailList_ShouldReturnReadOnlyList()
    {
        // Arrange
        var group = EmailGroup.Create("Test Group", _ownerId, "a@test.com, b@test.com, c@test.com").Value;

        // Act
        var emailList = group.GetEmailList();

        // Assert
        emailList.Should().HaveCount(3);
        emailList.Should().BeAssignableTo<IReadOnlyList<string>>();
    }

    #endregion
}
