using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using CleanHr.DepartmentApi.Application.Caching.Handlers;
using CleanHr.DepartmentApi.Application.Commands;
using CleanHr.DepartmentApi.Domain;
using CleanHr.DepartmentApi.Domain.Models;
using CleanHr.DepartmentApi.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace CleanHr.DepartmentApi.Application.Tests.CommandTests;

public class CreateDepartmentCommandTests
{
    private readonly Mock<IDepartmentRepository> _mockRepository;
    private readonly Mock<IDepartmentCacheHandler> _mockCacheHandler;
    private readonly Mock<ILogger<object>> _mockLogger;
    private readonly IMediator _mediator;

    public CreateDepartmentCommandTests()
    {
        _mockRepository = new Mock<IDepartmentRepository>();
        _mockCacheHandler = new Mock<IDepartmentCacheHandler>();
        _mockLogger = new Mock<ILogger<object>>();

        // Setup MediatR with the handler
        var services = new ServiceCollection();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<CreateDepartmentCommand>());
        services.AddSingleton(_mockRepository.Object);
        services.AddSingleton(_mockCacheHandler.Object);
        services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
        services.AddSingleton<ILoggerFactory, LoggerFactory>();

        var serviceProvider = services.BuildServiceProvider();
        _mediator = serviceProvider.GetRequiredService<IMediator>();
    }

    [Fact]
    public async Task ValidCommand_ReturnsSuccessWithDepartmentId()
    {
        // Arrange
        var command = new CreateDepartmentCommand("IT Department", "Information Technology");
        var departmentId = Guid.NewGuid();

        _mockRepository
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Department, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(false));

        _mockRepository
            .Setup(r => r.InsertAsync(It.IsAny<Department>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Department d, CancellationToken ct) => Result<Department>.Success(d));

        _mockCacheHandler
            .Setup(c => c.RemoveListAsync())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _mediator.Send(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);

        _mockRepository.Verify(r => r.InsertAsync(It.IsAny<Department>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockCacheHandler.Verify(c => c.RemoveListAsync(), Times.Once);
    }

    [Fact]
    public async Task DuplicateDepartmentName_ReturnsFailure()
    {
        // Arrange
        var command = new CreateDepartmentCommand("IT Department", "Information Technology");

        _mockRepository
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Department, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));

        // Act
        var result = await _mediator.Send(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);

        _mockRepository.Verify(r => r.InsertAsync(It.IsAny<Department>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockCacheHandler.Verify(c => c.RemoveListAsync(), Times.Never);
    }

    [Fact]
    public async Task NullCommand_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _mediator.Send(null, CancellationToken.None));
    }

    [Fact]
    public async Task EmptyName_ReturnsFailure()
    {
        // Arrange
        var command = new CreateDepartmentCommand("", "Valid Description");

        // Act
        var result = await _mediator.Send(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);

        _mockRepository.Verify(r => r.InsertAsync(It.IsAny<Department>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task NameTooLong_ReturnsFailure()
    {
        // Arrange
        var longName = new string('A', 101); // Assuming max length is 100
        var command = new CreateDepartmentCommand(longName, "Valid Description");

        // Act
        var result = await _mediator.Send(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);

        _mockRepository.Verify(r => r.InsertAsync(It.IsAny<Department>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RepositoryThrowsException_ReturnsFailureResult()
    {
        // Arrange
        var command = new CreateDepartmentCommand("IT Department", "Information Technology");

        _mockRepository
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Department, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(false));

        _mockRepository
            .Setup(r => r.InsertAsync(It.IsAny<Department>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act
        var result = await _mediator.Send(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Exception", result.Error);

        _mockCacheHandler.Verify(c => c.RemoveListAsync(), Times.Never);
    }

    [Fact]
    public async Task SuccessfulCreation_LogsInformation()
    {
        // Arrange
        var command = new CreateDepartmentCommand("IT Department", "Information Technology");

        _mockRepository
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Department, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(false));

        _mockRepository
            .Setup(r => r.InsertAsync(It.IsAny<Department>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Department d, CancellationToken ct) => Result<Department>.Success(d));

        _mockCacheHandler
            .Setup(c => c.RemoveListAsync())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _mediator.Send(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);

        // Verify logging occurred
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Creating department")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task SuccessfulCreation_RemovesCache()
    {
        // Arrange
        var command = new CreateDepartmentCommand("IT Department", "Information Technology");

        _mockRepository
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Department, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(false));

        _mockRepository
            .Setup(r => r.InsertAsync(It.IsAny<Department>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Department d, CancellationToken ct) => Result<Department>.Success(d));

        _mockCacheHandler
            .Setup(c => c.RemoveListAsync())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _mediator.Send(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        _mockCacheHandler.Verify(c => c.RemoveListAsync(), Times.Once);
    }
}
