using FluentAssertions;
using LankaConnect.Domain.Common.Database;

namespace LankaConnect.Domain.Tests.Common.Database;

/// <summary>
/// TDD RED Phase: Tests for LoadBalancingConfiguration class
/// These tests should FAIL until we implement the LoadBalancingConfiguration class
/// Focus on cultural intelligence integration and enterprise-grade load balancing
/// </summary>
public class LoadBalancingConfigurationTests
{
    [Fact]
    public void LoadBalancingConfiguration_Create_ShouldCreateValidConfiguration()
    {
        // Arrange
        var configId = "lb-config-001";
        var strategy = LoadBalancingStrategy.CulturalAware;
        var healthCheckInterval = TimeSpan.FromSeconds(30);
        var maxRetries = 3;
        var culturalContext = "Sri Lankan Diaspora";
        
        // Act
        var config = LoadBalancingConfiguration.Create(configId, strategy, healthCheckInterval, maxRetries, culturalContext);
        
        // Assert
        config.Should().NotBeNull();
        config.ConfigurationId.Should().Be(configId);
        config.Strategy.Should().Be(strategy);
        config.HealthCheckInterval.Should().Be(healthCheckInterval);
        config.MaxRetries.Should().Be(maxRetries);
        config.CulturalContext.Should().Be(culturalContext);
    }
    
    [Fact]
    public void LoadBalancingConfiguration_CreateDefault_ShouldUseEnterpriseLevelDefaults()
    {
        // Arrange
        var configId = "default-lb-config";
        
        // Act
        var config = LoadBalancingConfiguration.CreateDefault(configId);
        
        // Assert
        config.Should().NotBeNull();
        config.ConfigurationId.Should().Be(configId);
        config.Strategy.Should().Be(LoadBalancingStrategy.CulturalAware);
        config.HealthCheckInterval.Should().Be(TimeSpan.FromSeconds(30));
        config.MaxRetries.Should().Be(3);
        config.IsEnterpriseGrade.Should().BeTrue();
        config.HasDisasterRecoveryIntegration.Should().BeTrue();
    }
    
    [Fact]
    public void LoadBalancingConfiguration_WithCulturalIntelligence_ShouldEnableCulturalRouting()
    {
        // Arrange
        var culturalProfiles = new Dictionary<string, CulturalLoadBalancingProfile>
        {
            ["Vesak"] = new CulturalLoadBalancingProfile("Vesak Festival", 2.0, TimeSpan.FromMinutes(5)),
            ["Diwali"] = new CulturalLoadBalancingProfile("Diwali Celebration", 1.8, TimeSpan.FromMinutes(4))
        };
        
        // Act
        var config = LoadBalancingConfiguration.Create("cultural-lb", LoadBalancingStrategy.CulturalAware, 
            TimeSpan.FromSeconds(15), 5, "Multi-Cultural Community");
        config = config.WithCulturalProfiles(culturalProfiles);
        
        // Assert
        config.HasCulturalProfiles.Should().BeTrue();
        config.CulturalProfiles.Should().HaveCount(2);
        config.GetCulturalProfile("Vesak").Should().NotBeNull();
        config.GetCulturalProfile("Vesak")!.TrafficMultiplier.Should().Be(2.0);
    }
    
    [Theory]
    [InlineData(LoadBalancingStrategy.RoundRobin, false)]
    [InlineData(LoadBalancingStrategy.WeightedRoundRobin, false)]
    [InlineData(LoadBalancingStrategy.LeastConnections, false)]
    [InlineData(LoadBalancingStrategy.CulturalAware, true)]
    [InlineData(LoadBalancingStrategy.DiasporaOptimized, true)]
    public void LoadBalancingConfiguration_WithDifferentStrategies_ShouldClassifyCorrectly(LoadBalancingStrategy strategy, bool expectsCultural)
    {
        // Arrange & Act
        var config = LoadBalancingConfiguration.Create("strategy-test", strategy, TimeSpan.FromSeconds(30), 3, null);
        
        // Assert
        config.IsCulturallyAware.Should().Be(expectsCultural);
        config.Strategy.Should().Be(strategy);
    }
    
    [Fact]
    public void LoadBalancingConfiguration_WithDisasterRecovery_ShouldConfigureFailoverSettings()
    {
        // Arrange
        var disasterRecoveryConfig = new DisasterRecoveryLoadBalancingConfiguration(
            TimeSpan.FromSeconds(10), // failover detection time
            TimeSpan.FromMinutes(30), // recovery window
            0.95 // health threshold
        );
        
        // Act
        var config = LoadBalancingConfiguration.CreateEnterpriseGrade("enterprise-lb")
            .WithDisasterRecoveryIntegration(disasterRecoveryConfig);
        
        // Assert
        config.HasDisasterRecoveryIntegration.Should().BeTrue();
        config.DisasterRecovery.Should().NotBeNull();
        config.DisasterRecovery!.FailoverDetectionTime.Should().Be(TimeSpan.FromSeconds(10));
        config.DisasterRecovery!.IsEnterpriseGrade.Should().BeTrue();
    }
    
    [Fact]
    public void LoadBalancingConfiguration_WithRevenueProtection_ShouldMaintainServiceDuringEvents()
    {
        // Arrange
        var revenueProtection = new RevenueProtectionLoadBalancingConfiguration(
            isEnabled: true,
            protectionThreshold: 0.99,
            emergencyCapacityPercent: 150
        );
        
        // Act
        var config = LoadBalancingConfiguration.Create("revenue-protected-lb", 
            LoadBalancingStrategy.CulturalAware, TimeSpan.FromSeconds(15), 5, "Revenue Critical Events")
            .WithRevenueProtection(revenueProtection);
        
        // Assert
        config.HasRevenueProtection.Should().BeTrue();
        config.RevenueProtection.Should().NotBeNull();
        config.RevenueProtection!.IsEnabled.Should().BeTrue();
        config.RevenueProtection!.ProtectionThreshold.Should().Be(0.99);
        config.CanHandleEmergencyCapacity.Should().BeTrue();
    }
    
    [Fact]
    public void LoadBalancingConfiguration_GetHealthCheckConfiguration_ShouldReturnValidSettings()
    {
        // Arrange
        var config = LoadBalancingConfiguration.Create("health-check-test", 
            LoadBalancingStrategy.LeastConnections, TimeSpan.FromSeconds(45), 2, null);
        
        // Act
        var healthConfig = config.GetHealthCheckConfiguration();
        
        // Assert
        healthConfig.Should().NotBeNull();
        healthConfig.Interval.Should().Be(TimeSpan.FromSeconds(45));
        healthConfig.MaxRetries.Should().Be(2);
        healthConfig.TimeoutThreshold.Should().BeGreaterThan(TimeSpan.Zero);
    }
    
    [Fact]
    public void LoadBalancingConfiguration_ValidateConfiguration_ShouldDetectInvalidSettings()
    {
        // Arrange
        var invalidConfig = LoadBalancingConfiguration.Create("invalid-test", 
            LoadBalancingStrategy.RoundRobin, TimeSpan.FromMilliseconds(100), 0, null);
        
        // Act
        var validation = invalidConfig.ValidateConfiguration();
        
        // Assert
        validation.IsValid.Should().BeFalse();
        validation.ValidationErrors.Should().NotBeEmpty();
        validation.ValidationErrors.Should().Contain(error => 
            error.Contains("HealthCheckInterval") || error.Contains("MaxRetries"));
    }
    
    [Fact]
    public void LoadBalancingConfiguration_CalculateOptimalCapacity_ShouldReturnCapacityRecommendation()
    {
        // Arrange
        var config = LoadBalancingConfiguration.CreateEnterpriseGrade("capacity-test")
            .WithCulturalProfiles(new Dictionary<string, CulturalLoadBalancingProfile>
            {
                ["SinhalaNewYear"] = new CulturalLoadBalancingProfile("Sinhala New Year", 3.0, TimeSpan.FromMinutes(10))
            });
        
        var currentLoad = new LoadMetrics(
            activeConnections: 1000,
            requestsPerSecond: 50,
            averageResponseTime: TimeSpan.FromMilliseconds(200)
        );
        
        // Act
        var capacityRecommendation = config.CalculateOptimalCapacity(currentLoad, "SinhalaNewYear");
        
        // Assert
        capacityRecommendation.Should().NotBeNull();
        capacityRecommendation.RecommendedCapacity.Should().BeGreaterThan(1000);
        capacityRecommendation.IsScalingRequired.Should().BeTrue();
        capacityRecommendation.CulturalEventContext.Should().Be("SinhalaNewYear");
    }
    
    [Fact]
    public void LoadBalancingConfiguration_Equality_ShouldBeEqualForSameConfiguration()
    {
        // Arrange
        var config1 = LoadBalancingConfiguration.Create("test-lb", LoadBalancingStrategy.CulturalAware, 
            TimeSpan.FromSeconds(30), 3, "Test Context");
        var config2 = LoadBalancingConfiguration.Create("test-lb", LoadBalancingStrategy.CulturalAware, 
            TimeSpan.FromSeconds(30), 3, "Test Context");
        
        // Act & Assert
        config1.Should().Be(config2);
        (config1 == config2).Should().BeTrue();
        config1.GetHashCode().Should().Be(config2.GetHashCode());
    }
    
    [Fact]
    public void LoadBalancingConfiguration_ToString_ShouldProvideReadableFormat()
    {
        // Arrange
        var config = LoadBalancingConfiguration.Create("readable-test", LoadBalancingStrategy.DiasporaOptimized,
            TimeSpan.FromSeconds(20), 4, "Global Diaspora");
        
        // Act
        var result = config.ToString();
        
        // Assert
        result.Should().NotBeEmpty();
        result.Should().Contain("readable-test");
        result.Should().Contain("DiasporaOptimized");
        result.Should().Contain("Global Diaspora");
    }
}