using LankaConnect.Modules.Forms.Application.Commands;
using LankaConnect.Modules.Forms.Domain.DomainEvents;
using LankaConnect.Modules.Forms.Domain.Entities;
using LankaConnect.Modules.Forms.Domain.Repositories;
using LankaConnect.Modules.Forms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace LankaConnect.Modules.Forms.Application.Tests.Commands;

/// <summary>
/// Wave 5.3d.2 (2026-06-12) — unit tests for <see cref="FormCommands"/>, the
/// cross-module Contracts mutator surface. The critical regression-guard test
/// is <c>DeleteResponsesByEventAndUser_RaisesDeletedEventBeforeDelete</c> — if
/// it disappears, the cancellation email + WhatsApp pipeline goes silent when
/// CancelRsvpCommandHandler routes through IFormCommands.
/// </summary>
public sealed class FormCommandsTests
{
    private readonly Mock<IFormResponseRepository> _repo;
    private readonly Mock<FormsDbContext> _formsContextMock;
    private readonly FormCommands _sut;

    public FormCommandsTests()
    {
        _repo = new Mock<IFormResponseRepository>();
        // Wave 6.5.d: FormCommands takes FormsDbContext. Class was unsealed in
        // the same commit so Moq can mock SaveChangesAsync (virtual on the
        // DbContext base). InMemory EF provider can't validate the FormsDbContext
        // model (jsonb-backed IReadOnlyList<Guid>) — hence Moq is preferred.
        var options = new DbContextOptionsBuilder<FormsDbContext>().Options;
        _formsContextMock = new Mock<FormsDbContext>(options);
        _formsContextMock
            .Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _sut = new FormCommands(_repo.Object, _formsContextMock.Object);
    }

    [Fact]
    public async Task DeleteResponsesByEventAndUser_NoMatches_ReturnsZero_AndDoesNotCallDelete()
    {
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _repo
            .Setup(r => r.GetByEventAndUserAsync(eventId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FormResponse>());

        var deleted = await _sut.DeleteResponsesByEventAndUserAsync(eventId, userId);

        deleted.Should().Be(0);
        _repo.Verify(
            r => r.DeleteAsync(It.IsAny<FormResponse>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task DeleteResponsesByEventAndUser_RaisesDeletedEventBeforeDelete()
    {
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var formId = Guid.NewGuid();
        var response = CreateResponse(eventId, formId, userId, "user@example.com");

        // Snapshot the response's domain events at the moment DeleteAsync is
        // invoked. If RaiseDeletedEvent() runs BEFORE DeleteAsync (the
        // contract), the snapshot contains FormResponseDeletedEvent. If it
        // runs AFTER (or not at all), the snapshot is empty and the
        // email/WhatsApp pipeline is silent in the cancel-RSVP path.
        var eventsAtDelete = new List<object[]>();

        _repo
            .Setup(r => r.GetByEventAndUserAsync(eventId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FormResponse> { response });
        _repo
            .Setup(r => r.DeleteAsync(response, It.IsAny<CancellationToken>()))
            .Callback<FormResponse, CancellationToken>((r, _) =>
                eventsAtDelete.Add(r.DomainEvents.Cast<object>().ToArray()))
            .Returns(Task.CompletedTask);

        var deleted = await _sut.DeleteResponsesByEventAndUserAsync(eventId, userId);

        deleted.Should().Be(1);
        eventsAtDelete.Should().HaveCount(1);
        eventsAtDelete[0].Should().ContainSingle()
            .Which.Should().BeOfType<FormResponseDeletedEvent>();
        var raised = (FormResponseDeletedEvent)eventsAtDelete[0][0];
        raised.FormId.Should().Be(formId);
        raised.ResponseId.Should().Be(response.Id);
        raised.RespondentEmail.Should().Be("user@example.com");
    }

    [Fact]
    public async Task DeleteResponsesByEventAndUser_MultipleResponses_RaisesEventAndDeletesEach()
    {
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var r1 = CreateResponse(eventId, Guid.NewGuid(), userId, "a@x.com");
        var r2 = CreateResponse(eventId, Guid.NewGuid(), userId, "b@x.com");

        _repo
            .Setup(r => r.GetByEventAndUserAsync(eventId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FormResponse> { r1, r2 });

        var deleted = await _sut.DeleteResponsesByEventAndUserAsync(eventId, userId);

        deleted.Should().Be(2);
        r1.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<FormResponseDeletedEvent>();
        r2.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<FormResponseDeletedEvent>();
        _repo.Verify(r => r.DeleteAsync(r1, It.IsAny<CancellationToken>()), Times.Once);
        _repo.Verify(r => r.DeleteAsync(r2, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static FormResponse CreateResponse(Guid eventId, Guid formId, Guid userId, string email)
    {
        var tokenHash = ComputeSha256("token-" + Guid.NewGuid());
        var result = FormResponse.Create(
            formId,
            eventId,
            tokenHash,
            null,
            email,
            "Test User",
            userId);
        result.IsSuccess.Should().BeTrue();
        var r = result.Value;
        typeof(FormResponse).GetProperty("Id")!.SetValue(r, Guid.NewGuid());
        r.ClearDomainEvents();
        return r;
    }

    private static string ComputeSha256(string input)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        var hash = System.Security.Cryptography.SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
