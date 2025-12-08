using System.Linq.Expressions;
using CleanHr.DepartmentApi.Domain.Models;
using CleanHr.DepartmentApi.Domain.Repositories;
using CleanHr.DepartmentApi.Domain.Validators;
using Moq;

namespace CleanHr.DepartmentApi.Domain.UnitTests.ValidatorTests;

public class DepartmentNameValidatorTests
{
    private readonly Mock<IDepartmentRepository> _mockRepository;

    public DepartmentNameValidatorTests()
    {
        _mockRepository = new Mock<IDepartmentRepository>();
    }

    [Theory]
    [InlineData("IT Department")] // Valid name
    [InlineData("AB")] // Name at minimum length (2 characters)
    [InlineData("AAAAAAAAAAAAAAAAAAAA")] // Name at maximum length (20 characters)
    public async Task ValidName_PassValidation(string departmentName)
    {
        // Arrange
        var departmentId = Guid.NewGuid();
        var validator = new DepartmentNameValidator(_mockRepository.Object, departmentId);

        _mockRepository
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Department, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(false));

        // Act
        var result = await validator.ValidateAsync(departmentName);

        // Assert
        Assert.True(result.IsValid);
    }


    [Theory]
    [InlineData(null, "The DepartmentName cannot be null.")] // Null name
    [InlineData("", "The DepartmentName cannot be empty.")] // Empty name
    [InlineData("   ", "The DepartmentName cannot be empty.")] // Whitespace name
    [InlineData("A", "The DepartmentName must be at least 2 characters.")] // Name too short
    [InlineData("AAAAAAAAAAAAAAAAAAAAA", "The DepartmentName can't be more than 20 characters.")] // Name too long
    public async Task InvalidName_FailsValidation(string invalidName, string errorMessage)
    {
        // Arrange
        var departmentId = Guid.NewGuid();
        var validator = new DepartmentNameValidator(_mockRepository.Object, departmentId);

        // Act
        var result = await validator.ValidateAsync(invalidName);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
        Assert.Contains(result.Errors, e => e.ErrorMessage == errorMessage);
    }

    [Fact]
    public async Task DuplicateName_FailsValidation()
    {
        // Arrange
        var departmentId = Guid.NewGuid();
        var validator = new DepartmentNameValidator(_mockRepository.Object, departmentId);

        _mockRepository
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Department, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true)); // Name already exists

        // Act
        var result = await validator.ValidateAsync("Existing Department");

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "The DepartmentName is already existent.");
    }


    [Fact]
    public async Task RepositoryReturnsFailure_FailsValidation()
    {
        // Arrange
        var departmentId = Guid.NewGuid();
        var validator = new DepartmentNameValidator(_mockRepository.Object, departmentId);

        _mockRepository
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Department, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Failure("Error", "Database error"));

        // Act
        var result = await validator.ValidateAsync("IT Department");

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "The DepartmentName is already existent.");
    }
}
