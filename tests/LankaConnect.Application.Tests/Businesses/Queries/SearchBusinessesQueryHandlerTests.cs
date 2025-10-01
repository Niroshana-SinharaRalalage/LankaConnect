using System.Linq.Expressions;
using AutoMapper;
using FluentAssertions;
using LankaConnect.Application.Businesses.Common;
using LankaConnect.Application.Businesses.Queries.SearchBusinesses;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Common.Models;
using LankaConnect.Application.Tests.TestHelpers;
using LankaConnect.Domain.Business;
using LankaConnect.Domain.Business.Enums;
using LankaConnect.Domain.Common;
using Moq;

namespace LankaConnect.Application.Tests.Businesses.Queries;

public class SearchBusinessesQueryHandlerTests
{
    private readonly Mock<IBusinessRepository> _businessRepository;
    private readonly Mock<IMapper> _mapper;
    private readonly SearchBusinessesQueryHandler _handler;

    public SearchBusinessesQueryHandlerTests()
    {
        _businessRepository = new Mock<IBusinessRepository>();
        _mapper = new Mock<IMapper>();
        _handler = new SearchBusinessesQueryHandler(_businessRepository.Object, _mapper.Object);
    }

    [Fact]
    public async Task Handle_WithValidQuery_ShouldReturnPaginatedBusinesses()
    {
        // Arrange
        var query = new SearchBusinessesQuery(
            SearchTerm: "restaurant",
            Category: "Restaurant",
            City: "Colombo",
            Province: null,
            Latitude: null,
            Longitude: null,
            RadiusKm: null,
            MinRating: null,
            IsVerified: null,
            PageNumber: 1,
            PageSize: 10
        );

        var businesses = new List<Business>
        {
            TestDataBuilder.CreateValidBusiness(Guid.NewGuid()),
            TestDataBuilder.CreateValidBusiness(Guid.NewGuid())
        };

        var businessDtos = new List<BusinessDto>
        {
            TestDataBuilder.CreateValidBusinessDto(),
            TestDataBuilder.CreateValidBusinessDto()
        };

        _businessRepository.Setup(x => x.GetPagedAsync(
            query.PageNumber, query.PageSize, It.IsAny<Expression<Func<Business, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((businesses, 2));

        _mapper.Setup(x => x.Map<List<BusinessDto>>(businesses))
            .Returns(businessDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(2);
        result.Value.PageNumber.Should().Be(1);
        result.Value.PageSize.Should().Be(10);

        _businessRepository.Verify(x => x.GetPagedAsync(
            query.PageNumber, query.PageSize, It.IsAny<Expression<Func<Business, bool>>>(), It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithEmptyResults_ShouldReturnEmptyPaginatedList()
    {
        // Arrange
        var query = new SearchBusinessesQuery(
            SearchTerm: "nonexistent",
            Category: null,
            City: null,
            Province: null,
            Latitude: null,
            Longitude: null,
            RadiusKm: null,
            MinRating: null,
            IsVerified: null,
            PageNumber: 1,
            PageSize: 10
        );

        _businessRepository.Setup(x => x.GetPagedAsync(
            query.PageNumber, query.PageSize, It.IsAny<Expression<Func<Business, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Business>(), 0));

        _mapper.Setup(x => x.Map<List<BusinessDto>>(It.IsAny<List<Business>>()))
            .Returns(new List<BusinessDto>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithNullSearchTerm_ShouldSearchAllBusinesses()
    {
        // Arrange
        var query = new SearchBusinessesQuery(
            SearchTerm: null,
            Category: null,
            City: null,
            Province: null,
            Latitude: null,
            Longitude: null,
            RadiusKm: null,
            MinRating: null,
            IsVerified: null,
            PageNumber: 1,
            PageSize: 10
        );

        var businesses = new List<Business> { TestDataBuilder.CreateValidBusiness(Guid.NewGuid()) };
        var businessDtos = new List<BusinessDto> { TestDataBuilder.CreateValidBusinessDto() };

        _businessRepository.Setup(x => x.GetPagedAsync(
            query.PageNumber, query.PageSize, It.IsAny<Expression<Func<Business, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((businesses, 1));

        _mapper.Setup(x => x.Map<List<BusinessDto>>(businesses))
            .Returns(businessDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_WithValidPageSize_ShouldRespectPagination()
    {
        // Arrange
        var query = new SearchBusinessesQuery(
            SearchTerm: "test",
            Category: null,
            City: null,
            Province: null,
            Latitude: null,
            Longitude: null,
            RadiusKm: null,
            MinRating: null,
            IsVerified: null,
            PageNumber: 2,
            PageSize: 5
        );

        var businesses = new List<Business> 
        { 
            TestDataBuilder.CreateValidBusiness(Guid.NewGuid()) 
        };
        var businessDtos = new List<BusinessDto> 
        { 
            TestDataBuilder.CreateValidBusinessDto() 
        };

        _businessRepository.Setup(x => x.GetPagedAsync(
            query.PageNumber, query.PageSize, It.IsAny<Expression<Func<Business, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((businesses, 1));

        _mapper.Setup(x => x.Map<List<BusinessDto>>(businesses))
            .Returns(businessDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PageNumber.Should().Be(2);
        result.Value.PageSize.Should().Be(5);
    }
}