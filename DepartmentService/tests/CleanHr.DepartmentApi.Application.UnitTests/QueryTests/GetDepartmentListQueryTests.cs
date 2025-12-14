using CleanHr.DepartmentApi.Application.Caching.Repositories;
using CleanHr.DepartmentApi.Application.Queries;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CleanHr.DepartmentApi.Application.UnitTests.QueryTests;

public class GetDepartmentListQueryTests
{
    private readonly Mock<IDepartmentCacheRepository> _mockCacheRepository;
    private readonly Mock<ILogger<object>> _mockLogger;
    private readonly IMediator _mediator;

    public GetDepartmentListQueryTests()
    {
        _mockCacheRepository = new Mock<IDepartmentCacheRepository>();
        _mockLogger = new Mock<ILogger<object>>();

        // Setup MediatR with the handler
        var services = new ServiceCollection();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<GetDepartmentListQuery>());
        services.AddSingleton(_mockCacheRepository.Object);

        // Register mock logger for verification
        var mockLoggerFactory = new Mock<ILoggerFactory>();
        mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(_mockLogger.Object);
        services.AddSingleton(mockLoggerFactory.Object);
        services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));

        var serviceProvider = services.BuildServiceProvider();
        _mediator = serviceProvider.GetRequiredService<IMediator>();
    }

    [Fact]
    public async Task ValidQuery_ReturnsDepartmentList()
    {
        // Arrange
        var query = new GetDepartmentListQuery();

        var expectedList = new List<DepartmentDto>
        {
            new() {
                Id = Guid.NewGuid(),
                Name = "IT Department",
                Description = "Information Technology",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            },
            new() {
                Id = Guid.NewGuid(),
                Name = "HR Department",
                Description = "Human Resources",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            }
        };

        _mockCacheRepository
            .Setup(r => r.GetListAsync())
            .ReturnsAsync(expectedList);

        // Act
        var result = await _mediator.Send(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.Count);
        Assert.Contains(result.Value, d => d.Name == "IT Department");
        Assert.Contains(result.Value, d => d.Name == "HR Department");

        _mockCacheRepository.Verify(r => r.GetListAsync(), Times.Once);

        // Verify logging occurred
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Department list retrieved successfully")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task EmptyList_ReturnsSuccessWithEmptyList()
    {
        // Arrange
        var query = new GetDepartmentListQuery();

        _mockCacheRepository
            .Setup(r => r.GetListAsync())
            .ReturnsAsync(new List<DepartmentDto>());

        // Act
        var result = await _mediator.Send(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value);

        _mockCacheRepository.Verify(r => r.GetListAsync(), Times.Once);

        // Verify logging with count 0
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Total Departments: 0")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task RepositoryThrowsException_ReturnsFailureResult()
    {
        // Arrange
        var query = new GetDepartmentListQuery();

        _mockCacheRepository
            .Setup(r => r.GetListAsync())
            .ThrowsAsync(new InvalidOperationException("Cache error"));

        // Act
        var result = await _mediator.Send(query, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("error occurred", result.Error, StringComparison.OrdinalIgnoreCase);

        // Verify error logging occurred
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error occurred while handling GetDepartmentListQuery")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task LargeList_ReturnsAllDepartments()
    {
        // Arrange
        var query = new GetDepartmentListQuery();

        var expectedList = new List<DepartmentDto>();
        for (int i = 1; i <= 100; i++)
        {
            expectedList.Add(new DepartmentDto
            {
                Id = Guid.NewGuid(),
                Name = $"Department {i}",
                Description = $"Description {i}",
                IsActive = i % 2 == 0,
                CreatedAtUtc = DateTime.UtcNow.AddDays(-i)
            });
        }

        _mockCacheRepository
            .Setup(r => r.GetListAsync())
            .ReturnsAsync(expectedList);

        // Act
        var result = await _mediator.Send(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(100, result.Value.Count);

        _mockCacheRepository.Verify(r => r.GetListAsync(), Times.Once);

        // Verify logging with correct count
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Total Departments: 100")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.AtLeastOnce);
    }
}
