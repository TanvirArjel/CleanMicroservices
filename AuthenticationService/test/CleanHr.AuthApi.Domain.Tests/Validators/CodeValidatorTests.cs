using System;
using System.Threading.Tasks;
using CleanHr.AuthApi.Domain.Validators;
using FluentAssertions;
using FluentValidation.Results;

namespace CleanHr.AuthApi.Domain.Tests.Validators;

public class CodeValidatorTests
{
    private readonly CodeValidator _validator;

    public CodeValidatorTests()
    {
        _validator = new CodeValidator();
    }

    [Fact]
    public async Task Validate_WithValidCode_ShouldReturnValid()
    {
        // Arrange
        string code = "123456";

        // Act
        ValidationResult result = await _validator.ValidateAsync(code);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Validate_WithEmptyCode_ShouldReturnInvalid(string code)
    {
        // Act
        ValidationResult result = await _validator.ValidateAsync(code);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "The Code is required.");
    }

    [Fact]
    public async Task Validate_WithNullCode_ShouldReturnInvalid()
    {
        // Act
        ValidationResult result = await _validator.ValidateAsync(null as string);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.ErrorMessage.Should().Be("The Code cannot be null.");
    }

    [Theory]
    [InlineData("12345")]    // Too short
    [InlineData("1234567")]  // Too long
    public async Task Validate_WithInvalidLength_ShouldReturnInvalid(string code)
    {
        // Act
        ValidationResult result = await _validator.ValidateAsync(code);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "The Code must be exactly 6 characters long.");
    }

    [Theory]
    [InlineData("12345a")]   // Contains letter
    [InlineData("12345!")]   // Contains special character
    [InlineData("12 456")]   // Contains space
    [InlineData("ABC123")]   // Contains letters
    public async Task Validate_WithNonDigits_ShouldReturnInvalid(string code)
    {
        // Act
        ValidationResult result = await _validator.ValidateAsync(code);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "The Code must contain only digits.");
    }

    [Theory]
    [InlineData("000000")]
    [InlineData("999999")]
    [InlineData("123456")]
    [InlineData("654321")]
    public async Task Validate_WithValidDigitCodes_ShouldReturnValid(string code)
    {
        // Act
        ValidationResult result = await _validator.ValidateAsync(code);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
