using CleanHr.DepartmentApi.Domain.Validators;

namespace CleanHr.DepartmentApi.Domain.UnitTests.ValidatorTests;

public class DepartmentDescriptionValidatorTests
{
    private readonly DepartmentDescriptionValidator _validator;

    public DepartmentDescriptionValidatorTests()
    {
        _validator = new DepartmentDescriptionValidator();
    }
    public static TheoryData<string> ValidDescriptionTestData => new()
    {
        // { validDescription }
        { "This is a valid description with sufficient length for validation." },
        { new string('A', 20) }, // Minimum length
        { new string('A', 200) } // Maximum length
    };

    public static TheoryData<string, string> InvalidDescriptionTestData => new()
    {
        // { invalidDescription, expectedErrorMessage }
        { null, "The Description is required." },
        { "", "The Description cannot be empty." },
        { "   ", "The Description cannot be empty." },
        { new string('A', 19), "The Description must be at least 20 characters." },
        { new string('A', 201), "The Description can't be more than 200 characters." }
    };


    [Theory]
    [MemberData(nameof(ValidDescriptionTestData))]
    public async Task ValidDescription_PassesValidation(string validDescription)
    {
        // Act
        var result = await _validator.ValidateAsync(validDescription);

        // Assert
        Assert.True(result.IsValid);
    }

    [Theory]
    [MemberData(nameof(InvalidDescriptionTestData))]
    public async Task InvalidDescription_FailsValidation(string invalidDescription, string errorMessage)
    {
        // Act
        var result = await _validator.ValidateAsync(invalidDescription);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
        Assert.Contains(result.Errors, e => e.ErrorMessage == errorMessage);
    }
}
