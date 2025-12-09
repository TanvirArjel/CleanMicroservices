using AutoFixture;
using CleanHr.DepartmentApi.Domain.Models;
using CleanHr.DepartmentApi.Persistence.RelationalDB;
using CleanHr.DepartmentApi.Persistence.RelationalDB.Repositories;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.EntityFrameworkCore;

namespace CleanHr.DepartmentApi.Persistence.RDB.UnitTests.RepositoryTests;

public class DepartmentRepositoryTests
{
    private static readonly Fixture Fixture = new Fixture();

    #region ExistsAsync Tests

    [Fact]
    public async Task ExistsAsync_WithExistingDepartment_ReturnsSuccessWithTrue()
    {
        // Arrange
        var department = CreateTestDepartment("IT Department");
        var departmentData = new List<Department>() { department };

        var mockLogger = new Mock<ILogger<DepartmentRepository>>();
        var mockDbContext = new Mock<CleanHrDbContext>();
        mockDbContext.Setup(m => m.Set<Department>()).ReturnsDbSet(departmentData);
        var repository = new DepartmentRepository(mockDbContext.Object, mockLogger.Object);

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
        var nonExistentId = Guid.NewGuid();
        var mockLogger = new Mock<ILogger<DepartmentRepository>>();
        var mockDbContext = new Mock<CleanHrDbContext>();
        mockDbContext.Setup(m => m.Set<Department>()).ReturnsDbSet(new List<Department>());
        var repository = new DepartmentRepository(mockDbContext.Object, mockLogger.Object);

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
        var mockLogger = new Mock<ILogger<DepartmentRepository>>();
        var mockDbContext = new Mock<CleanHrDbContext>();
        mockDbContext.Setup(m => m.Set<Department>()).ReturnsDbSet([department]);
        var repository = new DepartmentRepository(mockDbContext.Object, mockLogger.Object);

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
        var mockLogger = new Mock<ILogger<DepartmentRepository>>();
        var mockDbContext = new Mock<CleanHrDbContext>();
        mockDbContext.Setup(m => m.Set<Department>()).ReturnsDbSet([]);
        var repository = new DepartmentRepository(mockDbContext.Object, mockLogger.Object);
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
        var departmentData = new List<Department>() { department };

        var mockLogger = new Mock<ILogger<DepartmentRepository>>();
        var mockDbContext = new Mock<CleanHrDbContext>();
        mockDbContext.Setup(m => m.Set<Department>()).ReturnsDbSet(departmentData);
        var repository = new DepartmentRepository(mockDbContext.Object, mockLogger.Object);

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

        var mockLogger = new Mock<ILogger<DepartmentRepository>>();
        var mockDbContext = new Mock<CleanHrDbContext>();
        mockDbContext.Setup(m => m.Set<Department>()).ReturnsDbSet([department]);
        var repository = new DepartmentRepository(mockDbContext.Object, mockLogger.Object);

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
        var nonExistentId = Guid.NewGuid();
        var mockLogger = new Mock<ILogger<DepartmentRepository>>();
        var mockDbContext = new Mock<CleanHrDbContext>();
        mockDbContext.Setup(m => m.Set<Department>()).ReturnsDbSet([]);
        var repository = new DepartmentRepository(mockDbContext.Object, mockLogger.Object);


        // Act
        var result = await repository.GetByIdAsync(nonExistentId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Null(result.Value);
    }

    [Fact]
    public async Task GetByIdAsync_WithEmptyGuid_ReturnsFailure()
    {
        // Act
        var mockLogger = new Mock<ILogger<DepartmentRepository>>();
        var mockDbContext = new Mock<CleanHrDbContext>();
        mockDbContext.Setup(m => m.Set<Department>()).ReturnsDbSet([]);
        var repository = new DepartmentRepository(mockDbContext.Object, mockLogger.Object);

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
        //var department = CreateTestDepartment("Sales Department");

        var mockLogger = new Mock<ILogger<DepartmentRepository>>();
        var department = Fixture.Build<Department>().Create();
        var mockDbContext = new Mock<CleanHrDbContext>();
        mockDbContext.Setup(m => m.Set<Department>()).ReturnsDbSet([]);
        var repository = new DepartmentRepository(mockDbContext.Object, mockLogger.Object);

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
        var mockLogger = new Mock<ILogger<DepartmentRepository>>();
        var mockDbContext = new Mock<CleanHrDbContext>();
        mockDbContext.Setup(m => m.Set<Department>()).ReturnsDbSet([]);
        var repository = new DepartmentRepository(mockDbContext.Object, mockLogger.Object);

        // Act
        var result = await repository.InsertAsync(null);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task InsertAsync_MultipleDepartments_InsertsAllSuccessfully()
    {
        // Arrange
        var department1 = CreateTestDepartment("Marketing");
        var department2 = CreateTestDepartment("Operations");

        var mockLogger = new Mock<ILogger<DepartmentRepository>>();
        var mockDbContext = new Mock<CleanHrDbContext>();
        List<Department> _departmentData = [];
        mockDbContext.Setup(m => m.Set<Department>()).ReturnsDbSet(_departmentData);
        var repository = new DepartmentRepository(mockDbContext.Object, mockLogger.Object);

        // Act
        var result1 = await repository.InsertAsync(department1);
        var result2 = await repository.InsertAsync(department2);
        // Assert
        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);
        Assert.Equal(2, _departmentData.Count);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidDepartment_ReturnsSuccessWithUpdatedDepartment()
    {
        // Arrange
        var department = CreateTestDepartment("Legal");
        var mockLogger = new Mock<ILogger<DepartmentRepository>>();
        var mockDbContext = new Mock<CleanHrDbContext>();
        List<Department> _departmentData = [department];
        mockDbContext.Setup(m => m.Set<Department>()).ReturnsDbSet(_departmentData);
        var repository = new DepartmentRepository(mockDbContext.Object, mockLogger.Object);

        // Setup ChangeTracker mock to return null (non-tracked entity)
        var mockChangeTracker = new Mock<ChangeTracker>();
        mockDbContext.Setup(m => m.ChangeTracker).Returns(mockChangeTracker.Object);

        // Modify the department
        department.SetDescription("Updated description for legal department that meets the minimum length requirement.");
        department.IsActive = false;

        // Act
        var result = await repository.UpdateAsync(department);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        mockDbContext.Verify(m => m.Set<Department>().Update(department), Times.Once);
        mockDbContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithTrackedEntity_UpdatesSuccessfully()
    {
        // Arrange
        var department = CreateTestDepartment("Research");

        var mockLogger = new Mock<ILogger<DepartmentRepository>>();
        var mockDbContext = new Mock<CleanHrDbContext>();
        List<Department> _departmentData = [department];
        mockDbContext.Setup(m => m.Set<Department>()).ReturnsDbSet(_departmentData);
        var repository = new DepartmentRepository(mockDbContext.Object, mockLogger.Object);

        // Modify the department
        department.SetDescription("New description for research department that is long enough to pass validation.");
        department.IsActive = false;

        // Act
        var result = await repository.UpdateAsync(department);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        mockDbContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithNullDepartment_ReturnsFailure()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<DepartmentRepository>>();
        var mockDbContext = new Mock<CleanHrDbContext>();
        List<Department> _departmentData = [];
        mockDbContext.Setup(m => m.Set<Department>()).ReturnsDbSet(_departmentData);
        var repository = new DepartmentRepository(mockDbContext.Object, mockLogger.Object);

        // Act
        var result = await repository.UpdateAsync(null);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistingDepartment_InsertsNewDepartment()
    {
        // Arrange
        var department = CreateTestDepartment("New Dept");
        var mockLogger = new Mock<ILogger<DepartmentRepository>>();
        var mockDbContext = new Mock<CleanHrDbContext>();
        List<Department> _departmentData = [];
        mockDbContext.Setup(m => m.Set<Department>()).ReturnsDbSet(_departmentData);
        var repository = new DepartmentRepository(mockDbContext.Object, mockLogger.Object);

        // Act
        var result = await repository.UpdateAsync(department);

        // Assert
        Assert.True(result.IsSuccess);
        mockDbContext.Verify(m => m.Set<Department>().Update(department), Times.Once);
        mockDbContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithExistingDepartment_ReturnsSuccess()
    {
        // Arrange
        var department = CreateTestDepartment("Temporary");
        var mockLogger = new Mock<ILogger<DepartmentRepository>>();
        var mockDbContext = new Mock<CleanHrDbContext>();
        List<Department> _departmentData = [department];
        mockDbContext.Setup(m => m.Set<Department>()).ReturnsDbSet(_departmentData);
        var repository = new DepartmentRepository(mockDbContext.Object, mockLogger.Object);

        // Act
        var result = await repository.DeleteAsync(department);

        // Assert
        Assert.True(result.IsSuccess);
        mockDbContext.Verify(m => m.Set<Department>().Remove(department), Times.Once);
        mockDbContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithNullDepartment_ReturnsFailure()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<DepartmentRepository>>();
        var mockDbContext = new Mock<CleanHrDbContext>();
        List<Department> _departmentData = [];
        mockDbContext.Setup(m => m.Set<Department>()).ReturnsDbSet(_departmentData);
        var repository = new DepartmentRepository(mockDbContext.Object, mockLogger.Object);


        // Act
        var result = await repository.DeleteAsync(null);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task DeleteAsync_WithNonTrackedEntity_DeletesSuccessfully()
    {
        // Arrange
        var department = CreateTestDepartment("To Delete");
        var mockLogger = new Mock<ILogger<DepartmentRepository>>();
        var mockDbContext = new Mock<CleanHrDbContext>();
        List<Department> _departmentData = [department];
        mockDbContext.Setup(m => m.Set<Department>()).ReturnsDbSet(_departmentData);
        var repository = new DepartmentRepository(mockDbContext.Object, mockLogger.Object);


        // Act
        var result = await repository.DeleteAsync(department);

        // Assert
        Assert.True(result.IsSuccess);
        mockDbContext.Verify(m => m.Set<Department>().Remove(department), Times.Once);
        mockDbContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_MultipleDepartments_DeletesAllSuccessfully()
    {
        // Arrange
        var department1 = CreateTestDepartment("Dept 1");
        var department2 = CreateTestDepartment("Dept 2");
        List<Department> _departmentData = new List<Department> { department1, department2 };
        var mockLogger = new Mock<ILogger<DepartmentRepository>>();
        var mockDbContext = new Mock<CleanHrDbContext>();
        mockDbContext.Setup(m => m.Set<Department>()).ReturnsDbSet(_departmentData);
        var repository = new DepartmentRepository(mockDbContext.Object, mockLogger.Object);

        // Act
        var result1 = await repository.DeleteAsync(department1);
        var result2 = await repository.DeleteAsync(department2);

        // Assert
        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);
        Assert.Empty(_departmentData);
    }

    #endregion

    #region Helper Methods

    private Department CreateTestDepartment(string departmentName)
    {
        var department = (Department)Activator.CreateInstance(typeof(Department), true);

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
