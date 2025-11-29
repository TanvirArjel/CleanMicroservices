using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CleanHr.AuthApi.Domain.Models;
using CleanHr.AuthApi.Domain.Repositories;
using CleanHr.AuthApi.Domain.Validators;
using FluentAssertions;
using FluentValidation.Results;
using Moq;

namespace CleanHr.AuthApi.Domain.Tests.Validators;

public class ApplicationUserInputValidatorTests
{
    private readonly Mock<IApplicationUserRepository> _mockRepository;
    private readonly Guid _userId;

    public ApplicationUserInputValidatorTests()
    {
        _mockRepository = new Mock<IApplicationUserRepository>();
        _userId = Guid.NewGuid();
    }

    [Fact]
    public async Task Validate_WithAllValidInputs_ShouldReturnValid()
    {
        // Arrange
        _mockRepository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>()))
            .ReturnsAsync(false);

        var input = new ApplicationUserInput("ValidPass123", "test@example.com", "testuser");
        var validator = new ApplicationUserInputValidator(_userId, _mockRepository.Object);

        // Act
        ValidationResult result = await validator.ValidateAsync(input);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Validate_WithInvalidPassword_ShouldReturnInvalid()
    {
        // Arrange
        _mockRepository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>()))
            .ReturnsAsync(false);

        var input = new ApplicationUserInput("short", "test@example.com", "testuser");
        var validator = new ApplicationUserInputValidator(_userId, _mockRepository.Object);

        // Act
        ValidationResult result = await validator.ValidateAsync(input);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password" && e.ErrorMessage == "The Password must be at least 8 characters long.");
    }

    [Fact]
    public async Task Validate_WithInvalidEmail_ShouldReturnInvalid()
    {
        // Arrange
        _mockRepository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>()))
            .ReturnsAsync(false);

        var input = new ApplicationUserInput("ValidPass123", "invalid-email", "testuser");
        var validator = new ApplicationUserInputValidator(_userId, _mockRepository.Object);

        // Act
        ValidationResult result = await validator.ValidateAsync(input);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public async Task Validate_WithInvalidUserName_ShouldReturnInvalid()
    {
        // Arrange
        _mockRepository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>()))
            .ReturnsAsync(false);

        var input = new ApplicationUserInput("ValidPass123", "test@example.com", "ab"); // Too short
        var validator = new ApplicationUserInputValidator(_userId, _mockRepository.Object);

        // Act
        ValidationResult result = await validator.ValidateAsync(input);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UserName");
    }

    [Fact]
    public async Task Validate_WithMultipleInvalidInputs_ShouldReturnAllErrors()
    {
        // Arrange
        _mockRepository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>()))
            .ReturnsAsync(false);

        var input = new ApplicationUserInput("short", "invalid-email", "ab");
        var validator = new ApplicationUserInputValidator(_userId, _mockRepository.Object);

        // Act
        ValidationResult result = await validator.ValidateAsync(input);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterOrEqualTo(3); // At least one error for each field
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
        result.Errors.Should().Contain(e => e.PropertyName == "UserName");
    }

    [Fact]
    public async Task Validate_WithPasswordContainingWhitespace_ShouldReturnInvalid()
    {
        // Arrange
        _mockRepository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>()))
            .ReturnsAsync(false);

        var input = new ApplicationUserInput("Pass word123", "test@example.com", "testuser");
        var validator = new ApplicationUserInputValidator(_userId, _mockRepository.Object);

        // Act
        ValidationResult result = await validator.ValidateAsync(input);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password" && e.ErrorMessage == "The Password cannot contain whitespace.");
    }

    [Fact]
    public async Task Validate_WithPasswordContainingInvalidCharacters_ShouldReturnInvalid()
    {
        // Arrange
        _mockRepository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>()))
            .ReturnsAsync(false);

        var input = new ApplicationUserInput("Пароль123", "test@example.com", "testuser"); // Cyrillic characters
        var validator = new ApplicationUserInputValidator(_userId, _mockRepository.Object);

        // Act
        ValidationResult result = await validator.ValidateAsync(input);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password" && e.ErrorMessage == "The Password can only contain lowercase letters, uppercase letters, digits, and special characters.");
    }

    [Fact]
    public async Task Validate_WithAllNullInputs_ShouldReturnInvalid()
    {
        // Arrange
        _mockRepository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>()))
            .ReturnsAsync(false);

        var input = new ApplicationUserInput(null, null, null);
        var validator = new ApplicationUserInputValidator(_userId, _mockRepository.Object);

        // Act
        ValidationResult result = await validator.ValidateAsync(input);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterOrEqualTo(3); // At least one error for each null field
        result.Errors.Should().Contain(e => e.PropertyName == "Password" && e.ErrorMessage == "The Password cannot be null.");
        result.Errors.Should().Contain(e => e.PropertyName == "Email" && e.ErrorMessage == "The Email cannot be null.");
        result.Errors.Should().Contain(e => e.PropertyName == "UserName" && e.ErrorMessage == "The UserName cannot be null.");
    }
}
