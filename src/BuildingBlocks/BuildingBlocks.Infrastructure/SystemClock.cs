using LankaConnect.BuildingBlocks.Application.Abstractions;
namespace LankaConnect.BuildingBlocks.Infrastructure;

/// <summary>
/// Production <see cref="IClock"/> implementation backed by
/// <see cref="System.TimeProvider"/>.<c>System.GetUtcNow()</c>. Register as a
/// singleton in DI composition roots.
/// </summary>
/// <remarks>
/// Tests should inject a fake clock (see <c>BuildingBlocks.Testing</c> when
/// that csproj lands) rather than this implementation.
/// </remarks>
public sealed class SystemClock : IClock
{
    /// <summary>Shared singleton instance — safe to use directly without DI.</summary>
    public static readonly SystemClock Instance = new();

    /// <inheritdoc />
    public DateTime UtcNow => TimeProvider.System.GetUtcNow().UtcDateTime;
}
