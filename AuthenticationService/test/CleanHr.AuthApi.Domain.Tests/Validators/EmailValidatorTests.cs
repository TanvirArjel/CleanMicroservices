using System;
using System.Threading.Tasks;
using CleanHr.AuthApi.Domain.Validators;
using FluentAssertions;
using FluentValidation.Results;

namespace CleanHr.AuthApi.Domain.Tests.Validators;

public class EmailValidatorTests
{
    [Fact]
    public async Task Validate_WithValidEmail_ShouldReturnValid()
    {
        // Arrange
        EmailValidator validator = new();
        string email = "test@example.com";

        // Act
        ValidationResult result = await validator.ValidateAsync(email);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Validate_WithEmptyEmail_ShouldReturnInvalid(string email)
    {
        // Arrange
        EmailValidator validator = new();

        // Act
        ValidationResult result = await validator.ValidateAsync(email);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "The Email is required.");
    }

    [Fact]
    public async Task Validate_WithNullEmail_ShouldThrowException()
    {
        // Arrange
        EmailValidator validator = new();

        // Act
        ValidationResult result = await validator.ValidateAsync(null as string);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.ErrorMessage.Should().Be("The Email cannot be null.");
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("invalid@")]
    [InlineData("@example.com")]
    public async Task Validate_WithInvalidEmailFormat_ShouldReturnInvalid(string email)
    {
        // Arrange
        EmailValidator validator = new();

        // Act
        ValidationResult result = await validator.ValidateAsync(email);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "The Email is not a valid email.");
    }

    [Fact]
    public async Task Validate_WithEmailTooLong_ShouldReturnInvalid()
    {
        // Arrange
        EmailValidator validator = new();
        string email = "verylongemailaddressthatexceedsthelimit@example.com"; // More than 50 characters

        // Act
        ValidationResult result = await validator.ValidateAsync(email);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "The Email can't be more than 50 characters long.");
    }

    [Theory]
    [InlineData("user@example.com")]
    [InlineData("test.user@domain.co.uk")]
    [InlineData("name+tag@example.org")]
    public async Task Validate_WithValidEmailFormats_ShouldReturnValid(string email)
    {
        // Arrange
        EmailValidator validator = new();

        // Act
        ValidationResult result = await validator.ValidateAsync(email);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
