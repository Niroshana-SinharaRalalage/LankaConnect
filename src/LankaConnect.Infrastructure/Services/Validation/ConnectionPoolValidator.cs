using LankaConnect.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace LankaConnect.Infrastructure.Services.Validation;

/// <summary>
/// Phase 8 (post-prod-perf-RCA hygiene) — validates the Npgsql client-side
/// connection pool size against the Postgres flexible-server's
/// <c>max_connections</c> at startup, and emits a structured warning if the
/// math doesn't leave headroom for the deployed replica count.
///
/// The original prod incident on 2026-04-25 surfaced "slow reads can hold
/// connections for full 35s, starving small endpoints" — that risk is
/// amplified when <c>(MaxPoolSize × replicas)</c> approaches
/// <c>max_connections</c>. Burstable Postgres SKUs ship with very small
/// <c>max_connections</c> (50 on B1ms / B2s); a Container App scaled to
/// 2 replicas with the default <c>MaxPoolSize=50</c> per replica would peak
/// at 100 client connections, exceeding the server ceiling.
///
/// This validator runs once at boot (IHostedService.StartAsync), reads
/// <c>SHOW max_connections</c>, parses <c>MaxPoolSize</c> out of the
/// connection string, and logs Information for the healthy case + Warning
/// for the dangerous case. It NEVER blocks startup or throws — observability
/// only.
/// </summary>
public class ConnectionPoolValidator : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ConnectionPoolValidator> _logger;

    public ConnectionPoolValidator(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<ConnectionPoolValidator> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                _logger.LogWarning("[ConnectionPoolValidator] DefaultConnection is empty — skipping pool-size sanity check");
                return;
            }

            // Parse the connection string to get the client-side MaxPoolSize.
            var builder = new NpgsqlConnectionStringBuilder(connectionString);
            var clientMaxPoolSize = builder.MaxPoolSize;

            // Read server-side max_connections (single SELECT; fast).
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var serverMaxConn = await ReadServerMaxConnectionsAsync(dbContext, cancellationToken);

            if (serverMaxConn <= 0)
            {
                _logger.LogWarning("[ConnectionPoolValidator] Could not read max_connections from server — skipping headroom check");
                return;
            }

            // Headroom math: assume up to N replicas at peak (architect spec for prod is 2-5).
            // If the user hasn't told us the planned max, default to 2 — minimum reasonable
            // for a multi-replica app. Reading from config so deployment can override.
            var assumedReplicaCount = _configuration.GetValue<int?>("ConnectionPool:AssumedMaxReplicas") ?? 2;
            var peakClientConnections = clientMaxPoolSize * assumedReplicaCount;
            var headroomThreshold = (int)(serverMaxConn * 0.8);  // 80% utilisation ceiling

            _logger.LogInformation(
                "[ConnectionPoolValidator] client_MaxPoolSize={ClientMax}, assumed_max_replicas={Replicas}, peak_clients={Peak}, server_max_connections={ServerMax}, 80%_threshold={Threshold}",
                clientMaxPoolSize, assumedReplicaCount, peakClientConnections, serverMaxConn, headroomThreshold);

            if (peakClientConnections > headroomThreshold)
            {
                _logger.LogWarning(
                    "[ConnectionPoolValidator] [POOL-OVERFLOW-RISK] Peak client connections {Peak} exceeds 80% of server max_connections ({Threshold} of {ServerMax}). " +
                    "Lower MaxPoolSize on the connection string OR raise Postgres max_connections OR cap replicas. " +
                    "Formula: MaxPoolSize × replicas <= max_connections × 0.8",
                    peakClientConnections, headroomThreshold, serverMaxConn);
            }
            else
            {
                _logger.LogInformation(
                    "[ConnectionPoolValidator] [OK] Pool size has headroom: peak {Peak} <= threshold {Threshold} (server max_connections={ServerMax})",
                    peakClientConnections, headroomThreshold, serverMaxConn);
            }
        }
        catch (Exception ex)
        {
            // Never block startup. Observability only.
            _logger.LogWarning(ex, "[ConnectionPoolValidator] Failed to validate pool size — non-fatal");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static async Task<int> ReadServerMaxConnectionsAsync(
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        // SHOW max_connections returns a string like "50". Use FromSqlRaw with a
        // lightweight projection. Fall back to 0 on parse failure (caller treats
        // 0 as "skip the check").
        try
        {
            var conn = dbContext.Database.GetDbConnection();
            if (conn.State != System.Data.ConnectionState.Open)
            {
                await conn.OpenAsync(cancellationToken);
            }
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SHOW max_connections";
            var raw = await cmd.ExecuteScalarAsync(cancellationToken);
            return raw is string s && int.TryParse(s, out var n) ? n : 0;
        }
        catch
        {
            return 0;
        }
    }
}
