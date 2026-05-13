using Testcontainers.PostgreSql;

namespace LankaConnect.BuildingBlocks.Infrastructure.Tests.Integration;

/// <summary>
/// xUnit class fixture that spins up a Postgres 15 container before the first
/// test in the class and disposes it after the last. Reused across all tests
/// in the class so each class pays ~3s startup once.
/// </summary>
/// <remarks>
/// <para>
/// Requires Docker available. CI ubuntu-latest runners have Docker; local
/// Windows / macOS developers need Docker Desktop running. Tests that DON'T
/// require Postgres (the InMemory unit tests) should NOT use this fixture.
/// </para>
/// </remarks>
public sealed class PostgresFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:15-alpine")
            .WithDatabase("bb_infra_tests")
            .WithUsername("test")
            .WithPassword("test")
            .Build();

        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }
}
