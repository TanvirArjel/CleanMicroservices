using System;
using CleanHr.AuthApi.Domain.Validators;
using FluentAssertions;
using FluentValidation.Results;

namespace CleanHr.AuthApi.Domain.Tests.Validators;

public class PasswordValidatorTests
{
    private readonly PasswordValidator _validator;

    public PasswordValidatorTests()
    {
        _validator = new PasswordValidator();
    }

    [Fact]
    public void Validate_WithValidPassword_ShouldReturnValid()
    {
        // Arrange
        string password = "ValidPass123";

        // Act
        ValidationResult result = _validator.Validate(password);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyPassword_ShouldReturnInvalid(string password)
    {
        // Act
        ValidationResult result = _validator.Validate(password);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "The Password is required.");
    }

    [Fact]
    public void Validate_WithNullPassword_ShouldThrowException()
    {
        // Act
        ValidationResult result = _validator.Validate(null as string);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.ErrorMessage.Should().Be("The Password cannot be null.");
    }

    [Theory]
    [InlineData("Short1")]
    [InlineData("1234567")]
    public void Validate_WithPasswordTooShort_ShouldReturnInvalid(string password)
    {
        // Act
        ValidationResult result = _validator.Validate(password);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.ErrorMessage.Should().Be("The Password must be at least 8 characters long.");
    }

    [Fact]
    public void Validate_WithPasswordTooLong_ShouldReturnInvalid()
    {
        // Arrange
        string password = "ThisPasswordIsWayTooLongForValidation123";

        // Act
        ValidationResult result = _validator.Validate(password);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.ErrorMessage.Should().Be("The Password cannot be more than 20 characters.");
    }

    [Theory]
    [InlineData("Password1")]
    [InlineData("12345678")]
    [InlineData("TestPass123")]
    [InlineData("ValidPass!@#")]
    [InlineData("12345678901234567890")] // Exactly 20 characters
    public void Validate_WithValidPasswordLengths_ShouldReturnValid(string password)
    {
        // Act
        ValidationResult result = _validator.Validate(password);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("Pass word1")]
    [InlineData("Test Pass123")]
    [InlineData("Valid Pass!@#")]
    [InlineData(" Password1")]
    [InlineData("Password1 ")]
    [InlineData("Pass word")]
    public void Validate_WithPasswordContainingWhitespace_ShouldReturnInvalid(string password)
    {
        // Act
        ValidationResult result = _validator.Validate(password);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "The Password cannot contain whitespace.");
    }

    [Theory]
    [InlineData("Password123")]
    [InlineData("Test@123")]
    [InlineData("Valid!Pass#123")]
    [InlineData("abcdefgh")]
    [InlineData("ABCDEFGH")]
    [InlineData("12345678")]
    [InlineData("Pass@Word$2023")]
    [InlineData("!@#$%^&*()_+-=")]
    public void Validate_WithValidCharacters_ShouldReturnValid(string password)
    {
        // Act
        ValidationResult result = _validator.Validate(password);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("Пароль123")]     // Cyrillic characters
    [InlineData("密码123456")]      // Chinese characters
    [InlineData("パスワード123")]   // Japanese characters
    [InlineData("Password123\n")]  // Newline character
    [InlineData("Password123\t")]  // Tab character
    [InlineData("Contraseña1")]    // Spanish ñ
    public void Validate_WithInvalidCharacters_ShouldReturnInvalid(string password)
    {
        // Act
        ValidationResult result = _validator.Validate(password);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "The Password can only contain lowercase letters, uppercase letters, digits, and special characters.");
    }
}

