using LankaConnect.Modules.Forms.Application.Queries;
using LankaConnect.Modules.Forms.Contracts;
using LankaConnect.Modules.Forms.Domain;
using LankaConnect.Modules.Forms.Domain.Entities;
using LankaConnect.Modules.Forms.Domain.Repositories;
using Moq;

namespace LankaConnect.Modules.Forms.Application.Tests.Queries;

/// <summary>
/// Wave 5.3b (2026-06-11) — unit tests for <see cref="FormQueries"/>, the
/// Contracts-side adapter that wraps <see cref="IFormRepository"/> +
/// <see cref="IFormResponseRepository"/>. Moq mocks the repositories;
/// no DB access. Asserts wire-shape correctness of the Domain -> Contracts
/// projection (the failing case is silent ABI drift for cross-module
/// consumers in Wave 5.3d).
/// </summary>
public sealed class FormQueriesTests
{
    [Fact]
    public async Task GetByOwnerAsync_NonEventOwner_ReturnsEmpty_WithoutCallingRepository()
    {
        var formRepo = new Mock<IFormRepository>();
        var responseRepo = new Mock<IFormResponseRepository>();
        var sut = new FormQueries(formRepo.Object, responseRepo.Object);

        // No FormOwnerEntityTypeDto value other than Event exists today, but the
        // SUT's switch fallback must not throw -- it returns empty so future
        // owner kinds don't break callers.
        var result = await sut.GetByOwnerAsync((FormOwnerEntityTypeDto)999, Guid.NewGuid());

        result.Should().BeEmpty();
        formRepo.Verify(
            r => r.GetByEventIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetByOwnerAsync_EventOwner_ReturnsMappedForms()
    {
        var eventId = Guid.NewGuid();
        var form = Form.Create(eventId, "Survey").Value;

        var formRepo = new Mock<IFormRepository>();
        var responseRepo = new Mock<IFormResponseRepository>();
        formRepo
            .Setup(r => r.GetByEventIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Form> { form });
        responseRepo
            .Setup(r => r.GetCountByFormIdAsync(form.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        var sut = new FormQueries(formRepo.Object, responseRepo.Object);

        var result = await sut.GetByOwnerAsync(FormOwnerEntityTypeDto.Event, eventId);

        result.Should().HaveCount(1);
        var dto = result[0];
        dto.Id.Should().Be(form.Id);
        dto.OwnerEntityType.Should().Be(FormOwnerEntityTypeDto.Event);
        dto.OwnerEntityId.Should().Be(eventId,
            because: "OwnerEntityId mirrors EventId for Event-owned forms during the 5.2b transitional phase.");
        dto.Title.Should().Be("Survey");
        dto.Status.Should().Be(FormStatusDto.Draft);
        dto.ResponseCount.Should().Be(3);
    }

    [Fact]
    public async Task GetByIdAsync_NoMatchingForm_ReturnsNull()
    {
        var formRepo = new Mock<IFormRepository>();
        var responseRepo = new Mock<IFormResponseRepository>();
        formRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Form?)null);

        var sut = new FormQueries(formRepo.Object, responseRepo.Object);

        var result = await sut.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
        responseRepo.Verify(
            r => r.GetCountByFormIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_WithMatchingForm_ReturnsSummary()
    {
        var form = Form.Create(Guid.NewGuid(), "Survey").Value;

        var formRepo = new Mock<IFormRepository>();
        var responseRepo = new Mock<IFormResponseRepository>();
        formRepo
            .Setup(r => r.GetByIdAsync(form.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(form);
        responseRepo
            .Setup(r => r.GetCountByFormIdAsync(form.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        var sut = new FormQueries(formRepo.Object, responseRepo.Object);

        var result = await sut.GetByIdAsync(form.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(form.Id);
        result.ResponseCount.Should().Be(5);
    }

    [Fact]
    public async Task GetByIdWithQuestionsAsync_NoMatchingForm_ReturnsNull()
    {
        var formRepo = new Mock<IFormRepository>();
        var responseRepo = new Mock<IFormResponseRepository>();
        formRepo
            .Setup(r => r.GetByIdWithQuestionsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Form?)null);

        var sut = new FormQueries(formRepo.Object, responseRepo.Object);

        var result = await sut.GetByIdWithQuestionsAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdWithQuestionsAsync_ReturnsDetailWithQuestions()
    {
        var form = Form.Create(Guid.NewGuid(), "Survey").Value;
        form.AddQuestion("Q1?", LankaConnect.Modules.Forms.Domain.Enums.FormQuestionType.ShortText,
            isRequired: true, sortOrder: 0);

        var formRepo = new Mock<IFormRepository>();
        var responseRepo = new Mock<IFormResponseRepository>();
        formRepo
            .Setup(r => r.GetByIdWithQuestionsAsync(form.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(form);
        responseRepo
            .Setup(r => r.GetCountByFormIdAsync(form.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        var sut = new FormQueries(formRepo.Object, responseRepo.Object);

        var result = await sut.GetByIdWithQuestionsAsync(form.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(form.Id);
        result.Questions.Should().HaveCount(1);
        result.Questions[0].QuestionText.Should().Be("Q1?");
        result.Questions[0].QuestionType.Should().Be(FormQuestionTypeDto.ShortText);
        result.ResponseCount.Should().Be(2);
    }
}
