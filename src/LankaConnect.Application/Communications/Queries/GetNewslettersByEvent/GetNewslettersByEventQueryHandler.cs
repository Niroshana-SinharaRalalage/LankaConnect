using AutoMapper;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Communications.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Communications.Queries.GetNewslettersByEvent;

/// <summary>
/// Phase 6A.74 Part 3D: Query handler to get newsletters linked to an event
/// </summary>
public class GetNewslettersByEventQueryHandler : IQueryHandler<GetNewslettersByEventQuery, IReadOnlyList<NewsletterDto>>
{
    private readonly INewsletterRepository _newsletterRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetNewslettersByEventQueryHandler> _logger;

    public GetNewslettersByEventQueryHandler(
        INewsletterRepository newsletterRepository,
        IMapper mapper,
        ILogger<GetNewslettersByEventQueryHandler> logger)
    {
        _newsletterRepository = newsletterRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<NewsletterDto>>> Handle(GetNewslettersByEventQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[Phase 6A.74] GetNewslettersByEventQuery - Getting newsletters for event {EventId}", request.EventId);

        var newsletters = await _newsletterRepository.GetByEventAsync(request.EventId, cancellationToken);

        var result = _mapper.Map<IReadOnlyList<NewsletterDto>>(newsletters);

        _logger.LogInformation("[Phase 6A.74] Found {Count} newsletters for event {EventId}", result.Count, request.EventId);

        return Result<IReadOnlyList<NewsletterDto>>.Success(result);
    }
}
