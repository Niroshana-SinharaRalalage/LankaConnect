using System.Diagnostics;
using LankaConnect.Modules.Forms.Domain;
using LankaConnect.Modules.Forms.Domain.Entities;
using LankaConnect.Modules.Forms.Domain.Enums;
using LankaConnect.Modules.Forms.Domain.DomainEvents;
using LankaConnect.Modules.Forms.Domain.Repositories;
using LankaConnect.Modules.Forms.Infrastructure.Data;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Entities;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using LankaConnect.Products.LankaEvents.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Modules.Forms.Application.Commands.CreateEventForm;

public class CreateEventFormCommandHandler : ICommandHandler<CreateEventFormCommand, Guid>
{
    private readonly IFormRepository _eventFormRepository;
    private readonly IEventRepository _eventRepository;
    private readonly FormsDbContext _formsContext;
    private readonly ILogger<CreateEventFormCommandHandler> _logger;

    public CreateEventFormCommandHandler(
        IFormRepository eventFormRepository,
        IEventRepository eventRepository,
        FormsDbContext formsContext,
        ILogger<CreateEventFormCommandHandler> logger)
    {
        _eventFormRepository = eventFormRepository;
        _eventRepository = eventRepository;
        _formsContext = formsContext;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(CreateEventFormCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "CreateEventForm"))
        using (LogContext.PushProperty("EntityType", "Form"))
        using (LogContext.PushProperty("EventId", request.EventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "CreateEventForm START: EventId={EventId}, Title={Title}, QuestionCount={QuestionCount}",
                request.EventId, request.Title, request.Questions.Count);

            try
            {
                // Verify event exists
                var eventExists = await _eventRepository.ExistsAsync(
                    e => e.Id == request.EventId, cancellationToken);

                if (!eventExists)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "CreateEventForm FAILED: Event not found - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);
                    return Result<Guid>.Failure($"Event with ID {request.EventId} not found");
                }

                // Create Form aggregate.
                // Phase 6A.146: pass AllowAttendeesToViewResponses through to the
                // factory so the create-time toggle (default false) is honored.
                var formResult = Form.Create(
                    request.EventId,
                    request.Title,
                    request.Description,
                    request.AllowMultipleResponses,
                    request.ResponseDeadline,
                    request.MaxResponses,
                    request.AllowAttendeesToViewResponses);

                if (formResult.IsFailure)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "CreateEventForm FAILED: Form creation failed - EventId={EventId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.EventId, formResult.Error, stopwatch.ElapsedMilliseconds);
                    return Result<Guid>.Failure(formResult.Error);
                }

                var form = formResult.Value;

                // Add questions
                foreach (var questionItem in request.Questions)
                {
                    // Build options if provided
                    var options = new List<QuestionOption>();
                    if (questionItem.Options != null)
                    {
                        foreach (var optionItem in questionItem.Options)
                        {
                            var optionResult = QuestionOption.Create(optionItem.Text, optionItem.SortOrder);
                            if (optionResult.IsFailure)
                            {
                                stopwatch.Stop();
                                _logger.LogWarning(
                                    "CreateEventForm FAILED: Option creation failed - EventId={EventId}, OptionText={OptionText}, Error={Error}, Duration={ElapsedMs}ms",
                                    request.EventId, optionItem.Text, optionResult.Error, stopwatch.ElapsedMilliseconds);
                                return Result<Guid>.Failure(optionResult.Error);
                            }
                            options.Add(optionResult.Value);
                        }
                    }

                    var addQuestionResult = form.AddQuestion(
                        questionItem.QuestionText,
                        questionItem.QuestionType,
                        questionItem.IsRequired,
                        questionItem.SortOrder,
                        options.Count > 0 ? options : null,
                        questionItem.HelpText);

                    if (addQuestionResult.IsFailure)
                    {
                        stopwatch.Stop();
                        _logger.LogWarning(
                            "CreateEventForm FAILED: Question creation failed - EventId={EventId}, QuestionText={QuestionText}, Error={Error}, Duration={ElapsedMs}ms",
                            request.EventId, questionItem.QuestionText, addQuestionResult.Error, stopwatch.ElapsedMilliseconds);
                        return Result<Guid>.Failure(addQuestionResult.Error);
                    }
                }

                // Wave 8.5.h (D-01): direct-SaveChanges per Consult #25 Q6.
                await _eventFormRepository.AddAsync(form, cancellationToken);
                await _formsContext.SaveChangesAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "CreateEventForm COMPLETE: FormId={FormId}, EventId={EventId}, Title={Title}, QuestionCount={QuestionCount}, Duration={ElapsedMs}ms",
                    form.Id, request.EventId, request.Title, request.Questions.Count, stopwatch.ElapsedMilliseconds);

                return Result<Guid>.Success(form.Id);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "CreateEventForm FAILED: Exception occurred - EventId={EventId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }
    }
}
