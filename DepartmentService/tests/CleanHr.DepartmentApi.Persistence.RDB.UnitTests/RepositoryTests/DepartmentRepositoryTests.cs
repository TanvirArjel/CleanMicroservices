using CleanHr.DepartmentApi.Domain.Models;
using CleanHr.DepartmentApi.Persistence.RelationalDB;
using CleanHr.DepartmentApi.Persistence.RelationalDB.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.EntityFrameworkCore;

namespace CleanHr.DepartmentApi.Persistence.RDB.UnitTests.RepositoryTests;

public class DepartmentRepositoryTests
{
    #region ExistsAsync Tests

    [Fact]
    public async Task ExistsAsync_WithExistingDepartment_ReturnsSuccessWithTrue()
    {
        // Arrange
        var department = CreateTestDepartment("IT Department");
        List<Department> departmentData = [department];

        Mock<ILogger<DepartmentRepository>> mockLogger = new();
        DbContextOptions<CleanHrDbContext> mockOptions = new();
        Mock<CleanHrDbContext> mockDbContext = new(mockOptions);
        mockDbContext.Setup(m => m.Set<Department>()).ReturnsDbSet(departmentData);
        DepartmentRepository repository = new(mockDbContext.Object, mockLogger.Object);

        // Act
        var result = await repository.ExistsAsync(d => d.Id == department.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Value);
    }

    [Fact]
    public async Task ExistsAsync_WithNonExistingDepartment_ReturnsSuccessWithFalse()
    {
        // Arrange
        Guid nonExistentId = Guid.NewGuid();
        Mock<ILogger<DepartmentRepository>> mockLogger = new();
        DbContextOptions<CleanHrDbContext> mockOptions = new();
        Mock<CleanHrDbContext> mockDbContext = new(mockOptions);
        mockDbContext.Setup(m => m.Set<Department>()).ReturnsDbSet(new List<Department>());
        DepartmentRepository repository = new(mockDbContext.Object, mockLogger.Object);

        // Act
        var result = await repository.ExistsAsync(d => d.Id == nonExistentId);
        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.Value);
    }

    [Fact]
    public async Task ExistsAsync_WithNullCondition_ReturnsSuccessWithTrue_WhenDepartmentsExist()
    {
        // Arrange
        var department = CreateTestDepartment("IT Department");
        Mock<ILogger<DepartmentRepository>> mockLogger = new();
        DbContextOptions<CleanHrDbContext> mockOptions = new();
        Mock<CleanHrDbContext> mockDbContext = new(mockOptions);
        mockDbContext.Setup(m => m.Set<Department>()).ReturnsDbSet([department]);
        DepartmentRepository repository = new(mockDbContext.Object, mockLogger.Object);

        // Act
        var result = await repository.ExistsAsync(null);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Value);
    }

    [Fact]
    public async Task ExistsAsync_WithNullCondition_ReturnsSuccessWithFalse_WhenNoDepartmentsExist()
    {
        // Arrange
        Mock<ILogger<DepartmentRepository>> mockLogger = new();
        DbContextOptions<CleanHrDbContext> mockOptions = new();
        Mock<CleanHrDbContext> mockDbContext = new(mockOptions);
        mockDbContext.Setup(m => m.Set<Department>()).ReturnsDbSet([]);
        DepartmentRepository repository = new(mockDbContext.Object, mockLogger.Object);
        // Act
        var result = await repository.ExistsAsync(null);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.Value);
    }

    [Fact]
    public async Task ExistsAsync_WithComplexCondition_ReturnsCorrectResult()
    {
        // Arrange
        // Arrange
        var department = CreateTestDepartment("IT Department");
        List<Department> departmentData = [department];

        Mock<ILogger<DepartmentRepository>> mockLogger = new();
        DbContextOptions<CleanHrDbContext> mockOptions = new();
        Mock<CleanHrDbContext> mockDbContext = new(mockOptions);
        mockDbContext.Setup(m => m.Set<Department>()).ReturnsDbSet(departmentData);
        DepartmentRepository repository = new(mockDbContext.Object, mockLogger.Object);

        // Act
        var result = await repository.ExistsAsync(d => d.Name == department.Name && d.IsActive);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Value);
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ReturnsSuccessWithDepartment()
    {
        // Arrange
        var department = CreateTestDepartment("IT Department");

        Mock<ILogger<DepartmentRepository>> mockLogger = new();
        DbContextOptions<CleanHrDbContext> mockOptions = new();
        Mock<CleanHrDbContext> mockDbContext = new(mockOptions);
        mockDbContext.Setup(m => m.Set<Department>()).ReturnsDbSet([department]);
        DepartmentRepository repository = new(mockDbContext.Object, mockLogger.Object);

        // Act
        var result = await repository.GetByIdAsync(department.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(department.Id, result.Value.Id);
        Assert.Equal(department.Name, result.Value.Name);
        Assert.Equal(department.Description, result.Value.Description);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingId_ReturnsSuccessWithNull()
    {
        // Arrange
        Guid nonExistentId = Guid.NewGuid();
        Mock<ILogger<DepartmentRepository>> mockLogger = new();
        DbContextOptions<CleanHrDbContext> mockOptions = new();
        Mock<CleanHrDbContext> mockDbContext = new(mockOptions);
        mockDbContext.Setup(m => m.Set<Department>()).ReturnsDbSet([]);
        DepartmentRepository repository = new(mockDbContext.Object, mockLogger.Object);


        // Act
        var result = await repository.GetByIdAsync(nonExistentId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Null(result.Value);
    }

    [Fact]
    public async Task GetByIdAsync_WithEmptyGuid_ReturnsFailure()
    {
        // Arrange
        DbContextOptions<CleanHrDbContext> mockOptions = new();
        Mock<CleanHrDbContext> mockDbContext = new(mockOptions);
        mockDbContext.Setup(m => m.Set<Department>()).ReturnsDbSet([]);
        Mock<ILogger<DepartmentRepository>> mockLogger = new();

        DepartmentRepository repository = new(mockDbContext.Object, mockLogger.Object);

        // Act
        var result = await repository.GetByIdAsync(Guid.Empty);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);
        Assert.NotEmpty(result.Errors);
    }

    #endregion

    #region InsertAsync Tests

    [Fact]
    public async Task InsertAsync_WithValidDepartment_ReturnsSuccessWithInsertedDepartment()
    {
        // Arrange
        var department = CreateTestDepartment("Sales Department");

        DbContextOptions<CleanHrDbContext> mockOptions = new();
        Mock<CleanHrDbContext> mockDbContext = new(mockOptions);

        Mock<DbSet<Department>> mockDbSet = new();
        mockDbContext.Setup(c => c.Set<Department>()).Returns(mockDbSet.Object);
        Mock<ILogger<DepartmentRepository>> mockLogger = new();

        DepartmentRepository repository = new(mockDbContext.Object, mockLogger.Object);

        // Act
        var result = await repository.InsertAsync(department);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(department.Id, result.Value.Id);
        mockDbContext.Verify(m => m.Set<Department>().AddAsync(It.Is<Department>(y => y == department), It.IsAny<CancellationToken>()), Times.Once);
        mockDbContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InsertAsync_WithNullDepartment_ReturnsFailure()
    {
        // Arrange
        Mock<ILogger<DepartmentRepository>> mockLogger = new();
        DbContextOptions<CleanHrDbContext> mockOptions = new();
        Mock<CleanHrDbContext> mockDbContext = new(mockOptions);
        Mock<DbSet<Department>> mockDbSet = new();
        mockDbContext.Setup(m => m.Set<Department>()).Returns(mockDbSet.Object);
        DepartmentRepository repository = new(mockDbContext.Object, mockLogger.Object);

        // Act
        var result = await repository.InsertAsync(null);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);
        Assert.NotEmpty(result.Errors);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidDepartment_ReturnsSuccessWithUpdatedDepartment()
    {
        // Arrange
        var department = CreateTestDepartment("Legal");

        DbContextOptions<CleanHrDbContext> mockOptions = new();
        Mock<CleanHrDbContext> mockDbContext = new(mockOptions);

        Mock<DbSet<Department>> mockDbSet = new();
        mockDbContext.Setup(m => m.Set<Department>()).Returns(mockDbSet.Object);

        Mock<ILogger<DepartmentRepository>> mockLogger = new();
        DepartmentRepository repository = new(mockDbContext.Object, mockLogger.Object);


        // Act
        var result = await repository.UpdateAsync(department);

        // Assert
        mockDbContext.Verify(x => x.Set<Department>().Update(It.Is<Department>(y => y == department)), Times.Once);
        mockDbContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.True(result.IsSuccess, $"Expected success but got failure. Error: {result.Error}");
        Assert.NotNull(result.Value);
    }

    [Fact]
    public async Task UpdateAsync_WithNullDepartment_ReturnsFailure()
    {
        // Arrange
        DbContextOptions<CleanHrDbContext> mockOptions = new();
        Mock<CleanHrDbContext> mockDbContext = new(mockOptions);
        Mock<DbSet<Department>> mockDbSet = new();
        mockDbContext.Setup(m => m.Set<Department>()).Returns(mockDbSet.Object);

        Mock<ILogger<DepartmentRepository>> mockLogger = new();
        DepartmentRepository repository = new(mockDbContext.Object, mockLogger.Object);

        // Act
        var result = await repository.UpdateAsync(null);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistingDepartment_ThrowsException()
    {
        // Arrange
        var department = CreateTestDepartment("Legal");

        DbContextOptions<CleanHrDbContext> mockOptions = new();
        Mock<CleanHrDbContext> mockDbContext = new(mockOptions);

        Mock<DbSet<Department>> mockDbSet = new();
        mockDbContext.Setup(m => m.Set<Department>()).Returns(mockDbSet.Object);
        mockDbContext.Setup(m => m.SaveChangesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new DbUpdateException());

        Mock<ILogger<DepartmentRepository>> mockLogger = new();
        DepartmentRepository repository = new(mockDbContext.Object, mockLogger.Object);


        // Act
        var result = await repository.UpdateAsync(department);

        // Assert
        mockDbContext.Verify(x => x.Set<Department>().Update(It.Is<Department>(y => y == department)), Times.Once);
        mockDbContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.False(result.IsSuccess, $"Expected failure but got success. Error: {result.Error}");
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithExistingDepartment_ReturnsSuccess()
    {
        // Arrange
        var department = CreateTestDepartment("Legal");

        DbContextOptions<CleanHrDbContext> mockOptions = new();
        Mock<CleanHrDbContext> mockDbContext = new(mockOptions);

        Mock<DbSet<Department>> mockDbSet = new();
        mockDbContext.Setup(m => m.Set<Department>()).Returns(mockDbSet.Object);

        Mock<ILogger<DepartmentRepository>> mockLogger = new();
        DepartmentRepository repository = new(mockDbContext.Object, mockLogger.Object);


        // Act
        var result = await repository.DeleteAsync(department);

        // Assert
        mockDbContext.Verify(x => x.Set<Department>().Remove(It.Is<Department>(y => y == department)), Times.Once);
        mockDbContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.True(result.IsSuccess, $"Expected success but got failure. Error: {result.Error}");
        Assert.NotNull(result);
    }

    [Fact]
    public async Task DeleteAsync_WithNullDepartment_ReturnsFailure()
    {
        // Arrange
        DbContextOptions<CleanHrDbContext> mockOptions = new();
        Mock<CleanHrDbContext> mockDbContext = new(mockOptions);
        Mock<DbSet<Department>> mockDbSet = new();
        mockDbContext.Setup(m => m.Set<Department>()).Returns(mockDbSet.Object);

        Mock<ILogger<DepartmentRepository>> mockLogger = new();
        DepartmentRepository repository = new(mockDbContext.Object, mockLogger.Object);

        // Act
        var result = await repository.DeleteAsync(null);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistingDepartment_ThrowsException()
    {
        // Arrange
        var department = CreateTestDepartment("Legal");

        DbContextOptions<CleanHrDbContext> mockOptions = new();
        Mock<CleanHrDbContext> mockDbContext = new(mockOptions);

        Mock<DbSet<Department>> mockDbSet = new();
        mockDbContext.Setup(m => m.Set<Department>()).Returns(mockDbSet.Object);
        mockDbContext.Setup(m => m.SaveChangesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new DbUpdateException());

        Mock<ILogger<DepartmentRepository>> mockLogger = new();
        DepartmentRepository repository = new(mockDbContext.Object, mockLogger.Object);


        // Act
        var result = await repository.DeleteAsync(department);

        // Assert
        mockDbContext.Verify(x => x.Set<Department>().Remove(It.Is<Department>(y => y == department)), Times.Once);
        mockDbContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.False(result.IsSuccess, $"Expected failure but got success. Error: {result.Error}");
    }

    #endregion

    #region Helper Methods

    private Department CreateTestDepartment(string departmentName)
    {
        Department department = (Department)Activator.CreateInstance(typeof(Department), true);

        // Use reflection to set private properties
        var idProperty = typeof(Department).GetProperty("Id");
        idProperty?.SetValue(department, Guid.NewGuid());

        var nameField = typeof(Department).GetField("<Name>k__BackingField",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        nameField?.SetValue(department, departmentName);

        var descriptionField = typeof(Department).GetField("<Description>k__BackingField",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        descriptionField?.SetValue(department, "Test description that meets minimum length requirements.");

        var isActiveProperty = typeof(Department).GetProperty("IsActive");
        isActiveProperty?.SetValue(department, true);

        var createdAtUtcProperty = typeof(Department).GetProperty("CreatedAtUtc");
        createdAtUtcProperty?.SetValue(department, DateTime.UtcNow);

        return department;
    }

    #endregion
}
