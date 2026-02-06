using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.ReferenceData.Entities;
using LankaConnect.Domain.ReferenceData.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using System.Diagnostics;

namespace LankaConnect.Infrastructure.Data.Repositories.ReferenceData;

/// <summary>
/// Repository implementation for unified reference data
/// Phase 6A.47: Unified Reference Data Architecture
/// Phase 6A.X: Enhanced with comprehensive observability logging
/// </summary>
public class ReferenceDataRepository : IReferenceDataRepository
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<ReferenceDataRepository> _logger;

    public ReferenceDataRepository(
        IApplicationDbContext context,
        ILogger<ReferenceDataRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    // Unified operations
    public async Task<IReadOnlyList<ReferenceValue>> GetByTypeAsync(
        string enumType,
        bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByType"))
        using (LogContext.PushProperty("EntityType", "ReferenceData"))
        using (LogContext.PushProperty("EnumType", enumType))
        using (LogContext.PushProperty("ActiveOnly", activeOnly))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation("GetByTypeAsync START: EnumType={EnumType}, ActiveOnly={ActiveOnly}", enumType, activeOnly);

            try
            {
                var query = _context.ReferenceValues
                    .Where(rv => rv.EnumType == enumType);

                if (activeOnly)
                {
                    query = query.Where(rv => rv.IsActive);
                }

                var result = await query
                    .OrderBy(rv => rv.DisplayOrder)
                    .ThenBy(rv => rv.Name)
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "GetByTypeAsync COMPLETE: EnumType={EnumType}, ActiveOnly={ActiveOnly}, Count={Count}, Duration={ElapsedMs}ms",
                    enumType,
                    activeOnly,
                    result.Count,
                    stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "GetByTypeAsync FAILED: EnumType={EnumType}, ActiveOnly={ActiveOnly}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    enumType,
                    activeOnly,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<IReadOnlyList<ReferenceValue>> GetByTypesAsync(
        IEnumerable<string> enumTypes,
        bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByTypes"))
        using (LogContext.PushProperty("EntityType", "ReferenceData"))
        using (LogContext.PushProperty("ActiveOnly", activeOnly))
        {
            var typeList = enumTypes.ToList();
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "GetByTypesAsync START: EnumTypeCount={EnumTypeCount}, ActiveOnly={ActiveOnly}",
                typeList.Count, activeOnly);

            try
            {
                var query = _context.ReferenceValues
                    .Where(rv => typeList.Contains(rv.EnumType));

                if (activeOnly)
                {
                    query = query.Where(rv => rv.IsActive);
                }

                var result = await query
                    .OrderBy(rv => rv.EnumType)
                    .ThenBy(rv => rv.DisplayOrder)
                    .ThenBy(rv => rv.Name)
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "GetByTypesAsync COMPLETE: EnumTypeCount={EnumTypeCount}, ActiveOnly={ActiveOnly}, Count={Count}, Duration={ElapsedMs}ms",
                    typeList.Count,
                    activeOnly,
                    result.Count,
                    stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "GetByTypesAsync FAILED: EnumTypeCount={EnumTypeCount}, ActiveOnly={ActiveOnly}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    typeList.Count,
                    activeOnly,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<ReferenceValue?> GetByTypeAndCodeAsync(
        string enumType,
        string code,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByTypeAndCode"))
        using (LogContext.PushProperty("EntityType", "ReferenceData"))
        using (LogContext.PushProperty("EnumType", enumType))
        using (LogContext.PushProperty("Code", code))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation("GetByTypeAndCodeAsync START: EnumType={EnumType}, Code={Code}", enumType, code);

            try
            {
                var result = await _context.ReferenceValues
                    .AsNoTracking()
                    .FirstOrDefaultAsync(rv => rv.EnumType == enumType && rv.Code == code, cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "GetByTypeAndCodeAsync COMPLETE: EnumType={EnumType}, Code={Code}, Found={Found}, Duration={ElapsedMs}ms",
                    enumType,
                    code,
                    result != null,
                    stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "GetByTypeAndCodeAsync FAILED: EnumType={EnumType}, Code={Code}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    enumType,
                    code,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<ReferenceValue?> GetByTypeAndIntValueAsync(
        string enumType,
        int intValue,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByTypeAndIntValue"))
        using (LogContext.PushProperty("EntityType", "ReferenceData"))
        using (LogContext.PushProperty("EnumType", enumType))
        using (LogContext.PushProperty("IntValue", intValue))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation("GetByTypeAndIntValueAsync START: EnumType={EnumType}, IntValue={IntValue}", enumType, intValue);

            try
            {
                var result = await _context.ReferenceValues
                    .AsNoTracking()
                    .FirstOrDefaultAsync(rv => rv.EnumType == enumType && rv.IntValue == intValue, cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "GetByTypeAndIntValueAsync COMPLETE: EnumType={EnumType}, IntValue={IntValue}, Found={Found}, Duration={ElapsedMs}ms",
                    enumType,
                    intValue,
                    result != null,
                    stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "GetByTypeAndIntValueAsync FAILED: EnumType={EnumType}, IntValue={IntValue}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    enumType,
                    intValue,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<ReferenceValue?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetById"))
        using (LogContext.PushProperty("EntityType", "ReferenceData"))
        using (LogContext.PushProperty("Id", id))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation("GetByIdAsync START: Id={Id}", id);

            try
            {
                var result = await _context.ReferenceValues
                    .AsNoTracking()
                    .FirstOrDefaultAsync(rv => rv.Id == id, cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "GetByIdAsync COMPLETE: Id={Id}, Found={Found}, Duration={ElapsedMs}ms",
                    id,
                    result != null,
                    stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "GetByIdAsync FAILED: Id={Id}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    id,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    // DEPRECATED: Legacy methods for backward compatibility
    // NOTE: These methods are no longer implemented because the old tables (event_categories, event_statuses, user_roles) were dropped
    // The service layer now uses GetByTypeAsync("EventCategory"/"EventStatus"/"UserRole") and maps to legacy DTOs
    #pragma warning disable CS0618 // Type or member is obsolete
    public Task<IReadOnlyList<EventCategoryRef>> GetEventCategoriesAsync(
        bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Legacy repository methods are no longer supported. Old tables were dropped in Phase 6A.47. Use service layer instead.");
    }

    public Task<EventCategoryRef?> GetEventCategoryByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Legacy repository methods are no longer supported. Old tables were dropped in Phase 6A.47. Use service layer instead.");
    }

    public Task<EventCategoryRef?> GetEventCategoryByCodeAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Legacy repository methods are no longer supported. Old tables were dropped in Phase 6A.47. Use service layer instead.");
    }

    public Task<IReadOnlyList<EventStatusRef>> GetEventStatusesAsync(
        bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Legacy repository methods are no longer supported. Old tables were dropped in Phase 6A.47. Use service layer instead.");
    }

    public Task<EventStatusRef?> GetEventStatusByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Legacy repository methods are no longer supported. Old tables were dropped in Phase 6A.47. Use service layer instead.");
    }

    public Task<EventStatusRef?> GetEventStatusByCodeAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Legacy repository methods are no longer supported. Old tables were dropped in Phase 6A.47. Use service layer instead.");
    }

    public Task<IReadOnlyList<UserRoleRef>> GetUserRolesAsync(
        bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Legacy repository methods are no longer supported. Old tables were dropped in Phase 6A.47. Use service layer instead.");
    }

    public Task<UserRoleRef?> GetUserRoleByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Legacy repository methods are no longer supported. Old tables were dropped in Phase 6A.47. Use service layer instead.");
    }

    public Task<UserRoleRef?> GetUserRoleByCodeAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Legacy repository methods are no longer supported. Old tables were dropped in Phase 6A.47. Use service layer instead.");
    }
    #pragma warning restore CS0618 // Type or member is obsolete
}
