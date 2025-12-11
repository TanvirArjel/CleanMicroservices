using System.Linq.Expressions;
using CleanHr.DepartmentApi.Application.Queries;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using TanvirArjel.EFCore.GenericRepository;
using Xunit;

namespace CleanHr.DepartmentApi.Application.UnitTests.QueryTests;

public class GetDepartmentByIdQueryTests
{
    private readonly Mock<IQueryRepository> _mockRepository;
    private readonly Mock<ILogger<object>> _mockLogger;
    private readonly IMediator _mediator;

    public GetDepartmentByIdQueryTests()
    {
        _mockRepository = new Mock<IQueryRepository>();
        _mockLogger = new Mock<ILogger<object>>();

        // Setup MediatR with the handler
        var services = new ServiceCollection();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<GetDepartmentByIdQuery>());
        services.AddSingleton(_mockRepository.Object);

        // Register mock logger for verification
        var mockLoggerFactory = new Mock<ILoggerFactory>();
        mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(_mockLogger.Object);
        services.AddSingleton(mockLoggerFactory.Object);
        services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));

        var serviceProvider = services.BuildServiceProvider();
        _mediator = serviceProvider.GetRequiredService<IMediator>();
    }

    [Fact]
    public async Task ValidQuery_ReturnsDepartmentDetails()
    {
        // Arrange
        var departmentId = Guid.NewGuid();
        var query = new GetDepartmentByIdQuery(departmentId);

        var expectedDto = new DepartmentDetailsDto
        {
            Id = departmentId,
            Name = "IT Department",
            Description = "Information Technology",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            LastModifiedAtUtc = DateTime.UtcNow
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(
                departmentId,
                It.IsAny<Expression<Func<Domain.Models.Department, DepartmentDetailsDto>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        // Act
        var result = await _mediator.Send(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(departmentId, result.Value.Id);
        Assert.Equal("IT Department", result.Value.Name);

        _mockRepository.Verify(
            r => r.GetByIdAsync(
                departmentId,
                It.IsAny<Expression<Func<Domain.Models.Department, DepartmentDetailsDto>>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        // Verify logging occurred
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Retrieving department details")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.AtLeastOnce);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Department retrieved successfully") && v.ToString().Contains("IT Department")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task DepartmentNotFound_ReturnsSuccessWithNullValue()
    {
        // Arrange
        var departmentId = Guid.NewGuid();
        var query = new GetDepartmentByIdQuery(departmentId);

        _mockRepository
            .Setup(r => r.GetByIdAsync(
                departmentId,
                It.IsAny<Expression<Func<Domain.Models.Department, DepartmentDetailsDto>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((DepartmentDetailsDto)null);

        // Act
        var result = await _mediator.Send(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Null(result.Value);

        // Verify warning logged for not found
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Department not found")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task RepositoryThrowsException_ReturnsFailureResult()
    {
        // Arrange
        var departmentId = Guid.NewGuid();
        var query = new GetDepartmentByIdQuery(departmentId);

        _mockRepository
            .Setup(r => r.GetByIdAsync(
                departmentId,
                It.IsAny<Expression<Func<Domain.Models.Department, DepartmentDetailsDto>>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

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
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error occurred while retrieving department")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.AtLeastOnce);
    }
}
