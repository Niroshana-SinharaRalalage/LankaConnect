using LankaConnect.Modules.Communications.Application.WhatsApp.Queries.GetWhatsAppTemplates;
using LankaConnect.Modules.Communications.Domain;
using LankaConnect.Modules.Communications.Domain.Entities;
using LankaConnect.Modules.Communications.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Tests.Communications.WhatsApp.Queries;

public class GetWhatsAppTemplatesQueryHandlerTests
{
    private readonly Mock<IWhatsAppTemplateRepository> _mockTemplateRepo;
    private readonly Mock<ILogger<GetWhatsAppTemplatesQueryHandler>> _mockLogger;
    private readonly GetWhatsAppTemplatesQueryHandler _handler;

    public GetWhatsAppTemplatesQueryHandlerTests()
    {
        _mockTemplateRepo = new Mock<IWhatsAppTemplateRepository>();
        _mockLogger = new Mock<ILogger<GetWhatsAppTemplatesQueryHandler>>();

        _handler = new GetWhatsAppTemplatesQueryHandler(
            _mockTemplateRepo.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_TemplatesExist_ReturnsMappedDtos()
    {
        // Arrange
        var template1 = WhatsAppTemplate.Create(
            templateName: "event_reminder",
            displayName: "Event Reminder",
            category: WhatsAppTemplateCategory.Utility,
            bodyText: "Hello {{1}}, your event {{2}} is tomorrow!",
            parameterNames: new List<string> { "name", "event_name" },
            language: "en",
            headerText: "Reminder",
            footerText: "LankaConnect");
        template1.MarkApproved("meta-template-001");

        var template2 = WhatsAppTemplate.Create(
            templateName: "registration_confirm",
            displayName: "Registration Confirmation",
            category: WhatsAppTemplateCategory.Utility,
            bodyText: "Hi {{1}}, you are registered for {{2}}.",
            parameterNames: new List<string> { "name", "event_name" },
            language: "en");

        var templates = new List<WhatsAppTemplate> { template1, template2 };

        var query = new GetWhatsAppTemplatesQuery();

        _mockTemplateRepo
            .Setup(x => x.GetAllTemplatesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(templates);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var dtos = result.Value;
        dtos.Should().HaveCount(2);

        var dto1 = dtos[0];
        dto1.TemplateName.Should().Be("event_reminder");
        dto1.DisplayName.Should().Be("Event Reminder");
        dto1.Category.Should().Be(WhatsAppTemplateCategory.Utility);
        dto1.Status.Should().Be(WhatsAppTemplateStatus.Approved);
        dto1.ParameterCount.Should().Be(2);
        dto1.Language.Should().Be("en");
        dto1.IsApproved.Should().BeTrue();
        dto1.IsUsable.Should().BeTrue();
        dto1.HeaderText.Should().Be("Reminder");
        dto1.FooterText.Should().Be("LankaConnect");
        dto1.ParameterNames.Should().BeEquivalentTo(new[] { "name", "event_name" });

        var dto2 = dtos[1];
        dto2.TemplateName.Should().Be("registration_confirm");
        dto2.Status.Should().Be(WhatsAppTemplateStatus.Pending);
        dto2.IsApproved.Should().BeFalse();
        dto2.IsUsable.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_NoTemplates_ReturnsEmptyList()
    {
        // Arrange
        var query = new GetWhatsAppTemplatesQuery();

        _mockTemplateRepo
            .Setup(x => x.GetAllTemplatesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WhatsAppTemplate>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_RepositoryThrows_PropagatesException()
    {
        // Arrange
        var query = new GetWhatsAppTemplatesQuery();

        _mockTemplateRepo
            .Setup(x => x.GetAllTemplatesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB error"));

        // Act & Assert
        await _handler.Invoking(h => h.Handle(query, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("DB error");
    }
}
