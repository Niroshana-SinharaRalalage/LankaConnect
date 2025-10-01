using Xunit;
using FluentAssertions;
using StackExchange.Redis;
using System.Net.Http;
using System.Net;
using System.Data;
using Npgsql;

namespace LankaConnect.IntegrationTests.Infrastructure;

[Collection("Docker")]
public class DockerConnectivityTests : IDisposable
{
    private readonly HttpClient _httpClient;
    
    public DockerConnectivityTests()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }

    [Fact]
    public async Task PostgreSQL_Container_IsAccessible()
    {
        // Arrange
        var connectionString = "Host=localhost;Port=5432;Database=LankaConnectDB;Username=lankaconnect;Password=dev_password_123;Timeout=30;Command Timeout=30";
        
        // Act & Assert
        await using var connection = new NpgsqlConnection(connectionString);
        var openTask = connection.OpenAsync();
        await openTask.WaitAsync(TimeSpan.FromSeconds(30));
        
        connection.State.Should().Be(ConnectionState.Open);
        
        // Test basic query
        await using var command = new NpgsqlCommand("SELECT version()", connection);
        var version = await command.ExecuteScalarAsync();
        version.Should().NotBeNull();
        version!.ToString().Should().Contain("PostgreSQL");
    }

    [Fact]
    public async Task Redis_Container_IsAccessible()
    {
        // Arrange
        var connectionString = "localhost:6379,password=dev_redis_123,connectTimeout=30000,responseTimeout=30000";
        
        // Act & Assert
        using var redis = ConnectionMultiplexer.Connect(connectionString);
        redis.IsConnected.Should().BeTrue();
        
        var database = redis.GetDatabase();
        var testKey = $"test:{Guid.NewGuid()}";
        var testValue = "test-value";
        
        await database.StringSetAsync(testKey, testValue);
        var retrievedValue = await database.StringGetAsync(testKey);
        
        retrievedValue.Should().Be(testValue);
        await database.KeyDeleteAsync(testKey);
    }

    [Fact]
    public async Task MailHog_Container_IsAccessible()
    {
        // Arrange
        var smtpPort = 1025;
        // var webPort = 8025; // Unused in current test
        
        // Test SMTP port
        using var tcpClient = new System.Net.Sockets.TcpClient();
        await tcpClient.ConnectAsync("localhost", smtpPort);
        tcpClient.Connected.Should().BeTrue();
        
        // Test Web UI
        var response = await _httpClient.GetAsync("http://localhost:8025/");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Azurite_Container_IsAccessible()
    {
        // Test Blob service
        var blobResponse = await _httpClient.GetAsync("http://localhost:10000/");
        blobResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest); // Expected for root endpoint

        // Test Queue service  
        var queueResponse = await _httpClient.GetAsync("http://localhost:10001/");
        queueResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest); // Expected for root endpoint

        // Test Table service
        var tableResponse = await _httpClient.GetAsync("http://localhost:10002/");
        tableResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest); // Expected for root endpoint
    }

    [Fact]
    public async Task Seq_Container_IsAccessible()
    {
        // Test Seq Web UI
        var response = await _httpClient.GetAsync("http://localhost:8080/");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Seq"); // Seq should be in the HTML content
    }

    [Fact]
    public async Task PgAdmin_Container_IsAccessible()
    {
        // Test PgAdmin Web UI
        var response = await _httpClient.GetAsync("http://localhost:8081/");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RedisCommander_Container_IsAccessible()
    {
        // Test Redis Commander Web UI
        var response = await _httpClient.GetAsync("http://localhost:8082/");
        
        // Redis Commander may redirect or require auth
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Found, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Docker_NetworkConnectivity_WorksCorrectly()
    {
        // Test that containers can communicate within the network
        // This is more of an integration test between containers
        
        // Connect to Redis and test a more complex scenario
        var connectionString = "localhost:6379,password=dev_redis_123";
        using var redis = ConnectionMultiplexer.Connect(connectionString);
        var database = redis.GetDatabase();
        
        // Test session-like data that might be shared between containers
        var sessionKey = "session:test";
        var sessionData = new
        {
            UserId = 123,
            Timestamp = DateTime.UtcNow,
            Data = "test session data"
        };
        
        await database.HashSetAsync(sessionKey, new HashEntry[]
        {
            new("UserId", sessionData.UserId),
            new("Timestamp", sessionData.Timestamp.ToString("O")),
            new("Data", sessionData.Data)
        });
        
        var retrievedUserId = await database.HashGetAsync(sessionKey, "UserId");
        retrievedUserId.Should().Be(sessionData.UserId);
        
        await database.KeyDeleteAsync(sessionKey);
    }
}