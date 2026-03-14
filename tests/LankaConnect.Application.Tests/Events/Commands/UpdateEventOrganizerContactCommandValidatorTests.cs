using FluentValidation.TestHelper;
using LankaConnect.Application.Events.Commands.UpdateEventOrganizerContact;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Commands;

/// <summary>
/// Phase 6A.132: Validator tests for UpdateEventOrganizerContactCommand
/// </summary>
public class UpdateEventOrganizerContactCommandValidatorTests
{
    private readonly UpdateEventOrganizerContactCommandValidator _validator;

    public UpdateEventOrganizerContactCommandValidatorTests()
    {
        _validator = new UpdateEventOrganizerContactCommandValidator();
    }

    [Fact]
    public void Should_Pass_WhenCommandIsValid_SingleContact()
    {
        // Arrange
        var command = new UpdateEventOrganizerContactCommand(
            EventId: Guid.NewGuid(),
            PublishOrganizerContact: true,
            Contacts: new List<OrganizerContactRequest>
            {
                new("John Doe", "john@example.com", "+1-555-1234")
            });

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_WhenEventIdIsEmpty()
    {
        // Arrange
        var command = new UpdateEventOrganizerContactCommand(
            EventId: Guid.Empty,
            PublishOrganizerContact: true,
            Contacts: new List<OrganizerContactRequest>
            {
                new("John Doe", "john@example.com", "+1-555-1234")
            });

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.EventId)
            .WithErrorMessage("Event ID is required");
    }

    [Fact]
    public void Should_Fail_WhenContactsExceedMaxLimit_ElevenContacts()
    {
        // Arrange
        var contacts = Enumerable.Range(1, 11)
            .Select(i => new OrganizerContactRequest($"Contact {i}", $"c{i}@example.com"))
            .ToList();

        var command = new UpdateEventOrganizerContactCommand(
            EventId: Guid.NewGuid(),
            PublishOrganizerContact: true,
            Contacts: contacts);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Contacts)
            .WithErrorMessage("Maximum 10 organizer contacts allowed");
    }

    [Fact]
    public void Should_Pass_WhenContactsAtMaxLimit_TenContacts()
    {
        // Arrange
        var contacts = Enumerable.Range(1, 10)
            .Select(i => new OrganizerContactRequest($"Contact {i}", $"c{i}@example.com"))
            .ToList();

        var command = new UpdateEventOrganizerContactCommand(
            EventId: Guid.NewGuid(),
            PublishOrganizerContact: true,
            Contacts: contacts);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_WhenContactNameIsEmpty()
    {
        // Arrange
        var command = new UpdateEventOrganizerContactCommand(
            EventId: Guid.NewGuid(),
            PublishOrganizerContact: true,
            Contacts: new List<OrganizerContactRequest>
            {
                new("", "john@example.com")
            });

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveAnyValidationError()
            .WithErrorMessage("Contact name is required");
    }

    [Fact]
    public void Should_Fail_WhenContactNameExceeds200Characters()
    {
        // Arrange
        var command = new UpdateEventOrganizerContactCommand(
            EventId: Guid.NewGuid(),
            PublishOrganizerContact: true,
            Contacts: new List<OrganizerContactRequest>
            {
                new(new string('A', 201), "john@example.com")
            });

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveAnyValidationError()
            .WithErrorMessage("Contact name must be less than 200 characters");
    }

    [Fact]
    public void Should_Fail_WhenContactPhoneExceeds20Characters()
    {
        // Arrange
        var command = new UpdateEventOrganizerContactCommand(
            EventId: Guid.NewGuid(),
            PublishOrganizerContact: true,
            Contacts: new List<OrganizerContactRequest>
            {
                new("John Doe", "john@example.com", new string('1', 21))
            });

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveAnyValidationError()
            .WithErrorMessage("Phone number must be less than 20 characters");
    }
}
