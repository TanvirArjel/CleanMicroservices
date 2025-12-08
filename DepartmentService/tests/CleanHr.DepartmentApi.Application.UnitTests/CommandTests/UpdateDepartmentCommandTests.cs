using System.Linq.Expressions;
using CleanHr.DepartmentApi.Application.Caching.Handlers;
using CleanHr.DepartmentApi.Application.Commands;
using CleanHr.DepartmentApi.Domain;
using CleanHr.DepartmentApi.Domain.Models;
using CleanHr.DepartmentApi.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CleanHr.DepartmentApi.Application.UnitTests.CommandTests;

public class UpdateDepartmentCommandTests
{
    private readonly Mock<IDepartmentRepository> _mockRepository;
    private readonly Mock<IDepartmentCacheHandler> _mockCacheHandler;
    private readonly Mock<ILogger<object>> _mockLogger;
    private readonly IMediator _mediator;

    public UpdateDepartmentCommandTests()
    {
        _mockRepository = new Mock<IDepartmentRepository>();
        _mockCacheHandler = new Mock<IDepartmentCacheHandler>();
        _mockLogger = new Mock<ILogger<object>>();

        // Setup MediatR with the handler
        var services = new ServiceCollection();
        services.AddSingleton(_mockRepository.Object);
        services.AddSingleton(_mockCacheHandler.Object);

        // Register mock logger for verification
        var mockLoggerFactory = new Mock<ILoggerFactory>();
        mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(_mockLogger.Object);
        services.AddSingleton(mockLoggerFactory.Object);
        services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<UpdateDepartmentCommand>());

        var serviceProvider = services.BuildServiceProvider();
        _mediator = serviceProvider.GetRequiredService<IMediator>();
    }

    [Fact]
    public async Task ValidCommand_ReturnsSuccess()
    {
        // Arrange
        var departmentId = Guid.NewGuid();
        var command = new UpdateDepartmentCommand(departmentId, "Updated IT", "This is Updated Description for the new IT Department", true);

        var existingDepartment = CreateDepartment(departmentId, "IT Department", "Old Description");

        _mockRepository
            .Setup(r => r.GetByIdAsync(departmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Department>.Success(existingDepartment));

        _mockRepository
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Department, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(false));

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Department>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Department d, CancellationToken ct) => Result<Department>.Success(d));

        _mockCacheHandler
            .Setup(c => c.RemoveListAsync())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _mediator.Send(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Updated IT", existingDepartment.Name);
        Assert.Equal("This is Updated Description for the new IT Department", existingDepartment.Description);
        Assert.True(existingDepartment.IsActive);

        // Verify repository methods were called
        _mockRepository.Verify(r => r.GetByIdAsync(departmentId, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Department>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockCacheHandler.Verify(c => c.RemoveListAsync(), Times.Once);

        // Verify logging occurred
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Updating department")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.AtLeastOnce);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Department updated successfully")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task DepartmentNotFound_ReturnsFailure()
    {
        // Arrange
        var departmentId = Guid.NewGuid();
        var command = new UpdateDepartmentCommand(departmentId, "Updated IT", "Updated Description", true);

        _mockRepository
            .Setup(r => r.GetByIdAsync(departmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Department>.Success(null));

        // Act
        var result = await _mediator.Send(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.Error, StringComparison.OrdinalIgnoreCase);

        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Department>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockCacheHandler.Verify(c => c.RemoveListAsync(), Times.Never);
    }

    [Fact]
    public async Task DuplicateDepartmentName_ReturnsFailure()
    {
        // Arrange
        var departmentId = Guid.NewGuid();
        var command = new UpdateDepartmentCommand(departmentId, "Existing Department", "Updated Description", true);

        var existingDepartment = CreateDepartment(departmentId, "IT Department", "Old Description");

        _mockRepository
            .Setup(r => r.GetByIdAsync(departmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Department>.Success(existingDepartment));

        _mockRepository
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Department, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));

        // Act
        var result = await _mediator.Send(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);

        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Department>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockCacheHandler.Verify(c => c.RemoveListAsync(), Times.Never);
    }

    [Fact]
    public async Task EmptyName_ReturnsFailure()
    {
        // Arrange
        var departmentId = Guid.NewGuid();
        var command = new UpdateDepartmentCommand(departmentId, "", "Valid Description", true);

        var existingDepartment = CreateDepartment(departmentId, "IT Department", "Old Description");

        _mockRepository
            .Setup(r => r.GetByIdAsync(departmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Department>.Success(existingDepartment));

        // Act
        var result = await _mediator.Send(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);

        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Department>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RepositoryThrowsException_ReturnsFailureResult()
    {
        // Arrange
        var departmentId = Guid.NewGuid();
        var command = new UpdateDepartmentCommand(departmentId, "Updated IT", "Updated Description", true);

        _mockRepository
            .Setup(r => r.GetByIdAsync(departmentId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act
        var result = await _mediator.Send(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("error occurred", result.Error);

        _mockCacheHandler.Verify(c => c.RemoveListAsync(), Times.Never);

        // Verify error logging occurred
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Exception occurred while updating department")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task GetByIdReturnsFailure_ReturnsFailure()
    {
        // Arrange
        var departmentId = Guid.NewGuid();
        var command = new UpdateDepartmentCommand(departmentId, "Updated IT", "Updated Description", true);

        _mockRepository
            .Setup(r => r.GetByIdAsync(departmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Department>.Failure("Error", "Failed to retrieve department"));

        // Act
        var result = await _mediator.Send(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Failed to retrieve department", result.Error);

        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Department>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockCacheHandler.Verify(c => c.RemoveListAsync(), Times.Never);
    }

    // Helper method to create a Department instance using reflection
    private Department CreateDepartment(Guid id, string name, string description)
    {
        var department = (Department)Activator.CreateInstance(typeof(Department), true);

        typeof(Department).GetProperty("Id").SetValue(department, id);
        typeof(Department).GetProperty("Name").SetValue(department, name);
        typeof(Department).GetProperty("Description").SetValue(department, description);
        typeof(Department).GetProperty("IsActive").SetValue(department, true);

        return department;
    }
}
