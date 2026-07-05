using LankaConnect.BuildingBlocks.Application.Abstractions;
using MediatR;
using Microsoft.Extensions.Logging;
namespace LankaConnect.BuildingBlocks.Application.Behaviors;

/// <summary>
/// Wraps every <see cref="ICommand{TResponse}"/> in a database transaction:
/// begin → handler → commit on success, rollback on exception. Queries skip
/// this behavior because they're read-only by contract.
/// </summary>
/// <remarks>
/// <para>
/// The handler is expected to persist its changes through the module's
/// repositories (which use the same <c>DbContext</c> as the
/// <see cref="IUnitOfWork"/>); this behavior calls <c>CommitAsync</c> which
/// in turn flushes pending changes to the database within the transaction.
/// </para>
/// <para>
/// Exception escapes: re-thrown after rollback so the upstream
/// <see cref="LoggingBehavior{TRequest,TResponse}"/> + ProblemDetails handler
/// can translate to an HTTP response.
/// </para>
/// </remarks>
public sealed class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand<TResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger;

    public TransactionBehavior(
        IUnitOfWork unitOfWork,
        ILogger<TransactionBehavior<TRequest, TResponse>> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        await _unitOfWork.BeginAsync(cancellationToken);

        try
        {
            var response = await next();
            await _unitOfWork.CommitAsync(cancellationToken);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Transaction rolled back for {RequestName}: {ErrorMessage}",
                typeof(TRequest).Name,
                ex.Message);

            try
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
            }
            catch (Exception rollbackEx)
            {
                // Swallow the rollback exception (after logging) to preserve
                // the original handler exception, which is the real cause.
                _logger.LogError(
                    rollbackEx,
                    "Rollback FAILED for {RequestName} — leaving original exception to propagate",
                    typeof(TRequest).Name);
            }

            throw;
        }
    }
}
