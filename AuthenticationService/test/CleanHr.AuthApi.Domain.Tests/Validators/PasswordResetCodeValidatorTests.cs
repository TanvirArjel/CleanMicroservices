using System;
using System.Threading.Tasks;
using CleanHr.AuthApi.Domain.Models;
using CleanHr.AuthApi.Domain.Validators;
using FluentAssertions;
using FluentValidation.Results;

namespace CleanHr.AuthApi.Domain.Tests.Validators;

public class PasswordResetCodeValidatorTests
{
    private readonly PasswordResetCodeValidator _validator;

    public PasswordResetCodeValidatorTests()
    {
        _validator = new PasswordResetCodeValidator();
    }

    [Fact]
    public async Task Validate_WithValidPasswordResetCode_ShouldReturnValid()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        Result<PasswordResetCode> codeResult = await PasswordResetCode.CreateAsync(userId, "test@example.com", "123456");

        // Assert
        codeResult.IsSuccess.Should().BeTrue();

        // Act
        ValidationResult result = await _validator.ValidateAsync(codeResult.Value);

        // Assert validator
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Validate_WithEmptyUserId_ShouldReturnInvalid()
    {
        // Arrange
        Guid userId = Guid.Empty;
        Result<PasswordResetCode> codeResult = await PasswordResetCode.CreateAsync(userId, "test@example.com", "123456");

        // Assert - The factory should fail validation
        codeResult.IsSuccess.Should().BeFalse();
        codeResult.Errors.Should().ContainKey("UserId");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Validate_WithEmptyEmail_ShouldReturnInvalid(string email)
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        Result<PasswordResetCode> codeResult = await PasswordResetCode.CreateAsync(userId, email, "123456");

        // Assert - The factory should fail validation
        codeResult.IsSuccess.Should().BeFalse();
        codeResult.Errors.Should().ContainKey("Email");
    }

    [Fact]
    public async Task Validate_WithNullEmail_ShouldReturnInvalid()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        Result<PasswordResetCode> codeResult = await PasswordResetCode.CreateAsync(userId, null, "123456");

        // Assert - The factory should fail validation
        codeResult.IsSuccess.Should().BeFalse();
        codeResult.Errors.Should().ContainKey("Email");
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("invalid@")]
    [InlineData("@example.com")]
    public async Task Validate_WithInvalidEmail_ShouldReturnInvalid(string email)
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        Result<PasswordResetCode> codeResult = await PasswordResetCode.CreateAsync(userId, email, "123456");

        // Assert - The factory should fail validation
        codeResult.IsSuccess.Should().BeFalse();
        codeResult.Errors.Should().ContainKey("Email");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Validate_WithEmptyCode_ShouldReturnInvalid(string code)
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        Result<PasswordResetCode> codeResult = await PasswordResetCode.CreateAsync(userId, "test@example.com", code);

        // Assert - The factory should fail validation
        codeResult.IsSuccess.Should().BeFalse();
        codeResult.Errors.Should().ContainKey("Code");
    }

    [Fact]
    public async Task Validate_WithNullCode_ShouldReturnInvalid()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        Result<PasswordResetCode> codeResult = await PasswordResetCode.CreateAsync(userId, "test@example.com", null);

        // Assert - The factory should fail validation
        codeResult.IsSuccess.Should().BeFalse();
        codeResult.Errors.Should().ContainKey("Code");
    }

    [Theory]
    [InlineData("12345")] // Too short
    [InlineData("1234567")] // Too long
    [InlineData("12")] // Too short
    public async Task Validate_WithInvalidCodeLength_ShouldReturnInvalid(string code)
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        Result<PasswordResetCode> codeResult = await PasswordResetCode.CreateAsync(userId, "test@example.com", code);

        // Assert - The factory should fail validation
        codeResult.IsSuccess.Should().BeFalse();
        codeResult.Errors.Should().ContainKey("Code");
    }

    [Theory]
    [InlineData("abcdef")]
    [InlineData("12345a")]
    [InlineData("ABC123")]
    [InlineData("!@#$%^")]
    public async Task Validate_WithNonDigitCode_ShouldReturnInvalid(string code)
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        Result<PasswordResetCode> codeResult = await PasswordResetCode.CreateAsync(userId, "test@example.com", code);

        // Assert - The factory should fail validation
        codeResult.IsSuccess.Should().BeFalse();
        codeResult.Errors.Should().ContainKey("Code");
    }

    [Theory]
    [InlineData("123456")]
    [InlineData("000000")]
    [InlineData("999999")]
    [InlineData("123450")]
    public async Task Validate_WithValidDigitCode_ShouldReturnValid(string code)
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        Result<PasswordResetCode> codeResult = await PasswordResetCode.CreateAsync(userId, "test@example.com", code);

        // Assert factory success
        codeResult.IsSuccess.Should().BeTrue();

        // Act
        ValidationResult result = await _validator.ValidateAsync(codeResult.Value);

        // Assert validator
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithValidEmailAndCode_ShouldReturnValid()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        Result<PasswordResetCode> codeResult = await PasswordResetCode.CreateAsync(userId, "user@example.com", "789012");

        // Assert factory success
        codeResult.IsSuccess.Should().BeTrue();

        // Act
        ValidationResult result = await _validator.ValidateAsync(codeResult.Value);

        // Assert validator and values
        result.IsValid.Should().BeTrue();
        codeResult.Value.Email.Should().Be("user@example.com");
        codeResult.Value.Code.Should().Be("789012");
        codeResult.Value.SentAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Validate_WithSentAtUtcInPast_ShouldReturnValid()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        Result<PasswordResetCode> codeResult = await PasswordResetCode.CreateAsync(userId, "test@example.com", "123456");

        // Assert factory success
        codeResult.IsSuccess.Should().BeTrue();

        // Act
        ValidationResult result = await _validator.ValidateAsync(codeResult.Value);

        // Assert validator and date
        result.IsValid.Should().BeTrue();
        codeResult.Value.SentAtUtc.Should().BeBefore(DateTime.UtcNow.AddSeconds(1));
    }
}
