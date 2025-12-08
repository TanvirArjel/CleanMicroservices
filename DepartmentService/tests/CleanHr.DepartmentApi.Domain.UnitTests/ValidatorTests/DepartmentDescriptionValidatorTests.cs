using CleanHr.DepartmentApi.Domain.Validators;

namespace CleanHr.DepartmentApi.Domain.UnitTests.ValidatorTests;

public class DepartmentDescriptionValidatorTests
{
    private readonly DepartmentDescriptionValidator _validator;

    public DepartmentDescriptionValidatorTests()
    {
        _validator = new DepartmentDescriptionValidator();
    }

    [Theory]
    [InlineData("This is a valid description with sufficient length for validation.")]
    public async Task ValidDescription_PassesValidation(string validDescription)
    {
        // Arrange
        var description = validDescription;
        // Act
        var result = await _validator.ValidateAsync(description);

        // Assert
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(null, "The Description is required.")] // null case
    [InlineData("", "The Description cannot be empty.")] // empty string case
    [InlineData("   ", "The Description cannot be empty.")] // whitespace case
    [InlineData("Short 19 characters", "The Description must be at least 20 characters.")] // too short case
    public async Task InvalidDescription_FailsValidation(string invalidDescription, string errorMessage)
    {
        // Act
        var result = await _validator.ValidateAsync(invalidDescription);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
        Assert.Contains(result.Errors, e => e.ErrorMessage == errorMessage);
    }

    [Fact]
    public async Task DescriptionTooLong_FailsValidation()
    {
        // Arrange
        var longDescription = new string('A', 201); // 201 characters, max is 200

        // Act
        var result = await _validator.ValidateAsync(longDescription);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "The Description can't be more than 200 characters.");
    }
}
