using System.Linq.Expressions;
using CleanHr.DepartmentApi.Domain.Models;
using CleanHr.DepartmentApi.Domain.Repositories;
using Moq;

namespace CleanHr.DepartmentApi.Domain.UnitTests.ModelTests;

public class DepartmentTests
{
    private readonly Mock<IDepartmentRepository> _mockRepository;

    public DepartmentTests()
    {
        _mockRepository = new Mock<IDepartmentRepository>();
    }

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidInputs_ReturnsSuccessWithDepartment()
    {
        // Arrange
        var name = "IT Department";
        var description = "This is a valid description with sufficient length for validation.";

        _mockRepository
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Department, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(false));

        // Act
        var result = await Department.CreateAsync(_mockRepository.Object, name, description);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(name, result.Value.Name);
        Assert.Equal(description, result.Value.Description);
        Assert.True(result.Value.IsActive);
        Assert.NotEqual(Guid.Empty, result.Value.Id);
        Assert.True(result.Value.CreatedAtUtc <= DateTime.UtcNow);
        Assert.Null(result.Value.LastModifiedAtUtc);
    }

    [Fact]
    public async Task CreateAsync_WithNullRepository_ThrowsArgumentNullException()
    {
        // Arrange
        var name = "IT Department";
        var description = "This is a valid description with sufficient length for validation.";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => Department.CreateAsync(null!, name, description));
    }

    [Theory]
    [InlineData(null)]  // Null name
    [InlineData("")]    // Empty name
    [InlineData(" ")]  // Empty name
    [InlineData("A")]   // Too short (minimum is 2)
    [InlineData("AAAAAAAAAAAAAAAAAAAAA")] // Too long (maximum is 20)
    public async Task CreateAsync_WithInvalidName_ReturnsFailure(string invalidName)
    {
        // Arrange
        var description = "This is a valid description with sufficient length for validation.";

        _mockRepository
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Department, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(false));

        // Act
        var result = await Department.CreateAsync(_mockRepository.Object, invalidName, description);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateName_ReturnsFailure()
    {
        // Arrange
        var name = "IT Department";
        var description = "This is a valid description with sufficient length for validation.";

        _mockRepository
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Department, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true)); // Name already exists

        // Act
        var result = await Department.CreateAsync(_mockRepository.Object, name, description);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);
        Assert.NotEmpty(result.Errors);
        Assert.Contains(result.Errors, kvp => kvp.Value.Contains("The DepartmentName is already existent."));
    }

    [Theory]
    [InlineData(null)]    // Null description
    [InlineData("Short")] // Too short (minimum is 20)
    [InlineData("")]      // Empty description
    [InlineData(" ")]     // Whitespace description
    public async Task CreateAsync_WithInvalidDescription_ReturnsFailure(string invalidDescription)
    {
        // Arrange
        var name = "IT Department";
        var description = invalidDescription;
        _mockRepository
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Department, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(false));

        // Act
        var result = await Department.CreateAsync(_mockRepository.Object, name, description);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);
        Assert.NotEmpty(result.Errors);
    }

    #endregion

    #region SetNameAsync Tests

    [Fact]
    public async Task SetNameAsync_WithValidName_UpdatesNameAndReturnsSuccess()
    {
        // Arrange
        var department = await CreateValidDepartmentAsync("IT Department");
        var newName = "HR Department";

        _mockRepository
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Department, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(false));

        // Act
        var result = await department.Value.SetNameAsync(_mockRepository.Object, newName);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(newName, department.Value.Name);
        Assert.NotNull(department.Value.LastModifiedAtUtc);
        Assert.True(department.Value.LastModifiedAtUtc <= DateTime.UtcNow);
    }

    [Fact]
    public async Task SetNameAsync_WithNullRepository_ThrowsArgumentNullException()
    {
        // Arrange
        var department = await CreateValidDepartmentAsync("IT Department");
        var newName = "HR Department";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => department.Value.SetNameAsync(null!, newName));
    }

    [Theory]
    [InlineData(null)]  // Null name
    [InlineData("")]    // Empty name
    [InlineData(" ")]   // Whitespace name
    [InlineData("A")]   // Too short (minimum is 2)
    [InlineData("AAAAAAAAAAAAAAAAAAAAA")] // Too long (maximum is 20)
    public async Task SetNameAsync_WithInvalidName_ReturnsFailureAndDoesNotUpdateName(string invalidName)
    {
        // Arrange
        var department = await CreateValidDepartmentAsync("IT Department");
        var originalName = department.Value.Name;

        _mockRepository
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Department, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(false));

        // Act
        var result = await department.Value.SetNameAsync(_mockRepository.Object, invalidName);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(originalName, department.Value.Name);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task SetNameAsync_WithDuplicateName_ReturnsFailureAndDoesNotUpdateName()
    {
        // Arrange
        var department = await CreateValidDepartmentAsync("IT Department");
        var originalName = department.Value.Name;
        var duplicateName = "HR Department";

        _mockRepository
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Department, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true)); // Name already exists

        // Act
        var result = await department.Value.SetNameAsync(_mockRepository.Object, duplicateName);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(originalName, department.Value.Name);
        Assert.NotEmpty(result.Errors);
        Assert.Contains(result.Errors, kvp => kvp.Value.Contains("The DepartmentName is already existent."));
    }

    #endregion

    #region SetDescription Tests

    [Fact]
    public async Task SetDescription_WithValidDescription_UpdatesDescriptionAndReturnsSuccess()
    {
        // Arrange
        var department = await CreateValidDepartmentAsync("IT Department");
        var newDescription = "This is a new valid description with sufficient length for validation.";

        // Act
        var result = department.Value.SetDescription(newDescription);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(newDescription, department.Value.Description);
        Assert.NotNull(department.Value.LastModifiedAtUtc);
        Assert.True(department.Value.LastModifiedAtUtc <= DateTime.UtcNow);
    }

    [Theory]
    [InlineData(null)]     // Null description
    [InlineData("")]       // Empty description
    [InlineData(" ")]      // Whitespace description
    [InlineData("Short")]  // Too short (minimum is 20)
    public async Task SetDescription_WithInvalidDescription_ReturnsFailureAndDoesNotUpdateDescription(string invalidDescription)
    {
        // Arrange
        var department = await CreateValidDepartmentAsync("IT Department");
        var originalDescription = department.Value.Description;

        // Act
        var result = department.Value.SetDescription(invalidDescription);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(originalDescription, department.Value.Description);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task SetDescription_WithTooLongDescription_ReturnsFailureAndDoesNotUpdateDescription()
    {
        // Arrange
        var department = await CreateValidDepartmentAsync("IT Department");
        var originalDescription = department.Value.Description;
        var tooLongDescription = new string('A', 201); // Maximum is 200

        // Act
        var result = department.Value.SetDescription(tooLongDescription);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(originalDescription, department.Value.Description);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task SetDescription_WithWhitespaceDescription_ReturnsFailureAndDoesNotUpdateDescription()
    {
        // Arrange
        var department = await CreateValidDepartmentAsync("IT Department");
        var originalDescription = department.Value.Description;
        var whitespaceDescription = "                    "; // 20 spaces

        // Act
        var result = department.Value.SetDescription(whitespaceDescription);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(originalDescription, department.Value.Description);
        Assert.NotEmpty(result.Errors);
    }

    #endregion

    #region Helper Methods

    private async Task<Result<Department>> CreateValidDepartmentAsync(string name)
    {
        var description = "This is a valid description with sufficient length for validation.";

        _mockRepository
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Department, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(false));

        return await Department.CreateAsync(_mockRepository.Object, name, description);
    }

    #endregion
}
