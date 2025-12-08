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

public class DeleteDepartmentCommandTests
{
    private readonly Mock<IDepartmentRepository> _mockRepository;
    private readonly Mock<IDepartmentCacheHandler> _mockCacheHandler;
    private readonly Mock<ILogger<object>> _mockLogger;
    private readonly IMediator _mediator;

    public DeleteDepartmentCommandTests()
    {
        _mockRepository = new Mock<IDepartmentRepository>();
        _mockCacheHandler = new Mock<IDepartmentCacheHandler>();
        _mockLogger = new Mock<ILogger<object>>();

        // Setup MediatR with the handler
        var services = new ServiceCollection();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<DeleteDepartmentCommand>());
        services.AddSingleton(_mockRepository.Object);
        services.AddSingleton(_mockCacheHandler.Object);

        // Register mock logger for verification
        var mockLoggerFactory = new Mock<ILoggerFactory>();
        mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(_mockLogger.Object);
        services.AddSingleton(mockLoggerFactory.Object);
        services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));

        var serviceProvider = services.BuildServiceProvider();
        _mediator = serviceProvider.GetRequiredService<IMediator>();
    }

    [Fact]
    public async Task ValidCommand_ReturnsSuccess()
    {
        // Arrange
        var departmentId = Guid.NewGuid();
        var command = new DeleteDepartmentCommand(departmentId);

        var existingDepartment = CreateDepartment(departmentId, "IT Department", "Description");

        _mockRepository
            .Setup(r => r.GetByIdAsync(departmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Department>.Success(existingDepartment));

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<Department>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _mockCacheHandler
            .Setup(c => c.RemoveListAsync())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _mediator.Send(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);

        // Verify repository methods were called
        _mockRepository.Verify(r => r.GetByIdAsync(departmentId, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<Department>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockCacheHandler.Verify(c => c.RemoveListAsync(), Times.Once);

        // Verify logging occurred
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Deleting department")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.AtLeastOnce);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Deleted department")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task DepartmentNotFound_DoesNotDelete()
    {
        // Arrange
        var departmentId = Guid.NewGuid();
        var command = new DeleteDepartmentCommand(departmentId);

        _mockRepository
            .Setup(r => r.GetByIdAsync(departmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Department>.Success(null));

        _mockRepository
            .Setup(r => r.DeleteAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _mockCacheHandler
            .Setup(c => c.RemoveListAsync())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _mediator.Send(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Department not found", result.Error);

        _mockRepository.Verify(r => r.DeleteAsync(null, It.IsAny<CancellationToken>()), Times.Never);
        _mockCacheHandler.Verify(c => c.RemoveListAsync(), Times.Never);
    }

    [Fact]
    public async Task RepositoryThrowsException_ReturnsFailureResult()
    {
        // Arrange
        var departmentId = Guid.NewGuid();
        var command = new DeleteDepartmentCommand(departmentId);

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
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error occurred while deleting department")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task DeleteFails_DoesNotRemoveCache()
    {
        // Arrange
        var departmentId = Guid.NewGuid();
        var command = new DeleteDepartmentCommand(departmentId);

        var existingDepartment = CreateDepartment(departmentId, "IT Department", "Description");

        _mockRepository
            .Setup(r => r.GetByIdAsync(departmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Department>.Success(existingDepartment));

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<Department>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("DeleteError", "Failed to delete department"));

        // Act
        var result = await _mediator.Send(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Failed to delete department", result.Error);

        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<Department>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockCacheHandler.Verify(c => c.RemoveListAsync(), Times.Never);

        // Verify error logging occurred
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to delete department with ID")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.AtLeastOnce);
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
