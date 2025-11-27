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

public class EmailValidatorTests
{
    [Fact]
    public async Task Validate_WithValidEmail_ShouldReturnValid()
    {
        // Arrange
        Mock<IApplicationUserRepository> repositoryMock = new();
        EmailValidator validator = new(Guid.NewGuid(), repositoryMock.Object);
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
        Mock<IApplicationUserRepository> repositoryMock = new();
        EmailValidator validator = new(Guid.NewGuid(), repositoryMock.Object);

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
        Mock<IApplicationUserRepository> repositoryMock = new();
        EmailValidator validator = new(Guid.NewGuid(), repositoryMock.Object);

        // Act
        Func<Task> act = async () => await validator.ValidateAsync(null as string);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Cannot pass a null model*");
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("invalid@")]
    [InlineData("@example.com")]
    public async Task Validate_WithInvalidEmailFormat_ShouldReturnInvalid(string email)
    {
        // Arrange
        Mock<IApplicationUserRepository> repositoryMock = new();
        EmailValidator validator = new(Guid.NewGuid(), repositoryMock.Object);

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
        Mock<IApplicationUserRepository> repositoryMock = new();
        EmailValidator validator = new(Guid.NewGuid(), repositoryMock.Object);
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
        Mock<IApplicationUserRepository> repositoryMock = new();
        EmailValidator validator = new(Guid.NewGuid(), repositoryMock.Object);

        // Act
        ValidationResult result = await validator.ValidateAsync(email);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithUniqueEmail_ShouldReturnValid()
    {
        // Arrange
        Mock<IApplicationUserRepository> repositoryMock = new();
        repositoryMock
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>()))
            .ReturnsAsync(false);

        EmailValidator validator = new(Guid.NewGuid(), repositoryMock.Object);
        string email = "unique@example.com";

        // Act
        ValidationResult result = await validator.ValidateAsync(email);

        // Assert
        result.IsValid.Should().BeTrue();
        repositoryMock.Verify(r => r.ExistsAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>()), Times.Once);
    }

    [Fact]
    public async Task Validate_WithDuplicateEmail_ShouldReturnInvalid()
    {
        // Arrange
        Mock<IApplicationUserRepository> repositoryMock = new();
        repositoryMock
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>()))
            .ReturnsAsync(true);

        EmailValidator validator = new(Guid.NewGuid(), repositoryMock.Object);
        string email = "duplicate@example.com";

        // Act
        ValidationResult result = await validator.ValidateAsync(email);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "A user already exists with the provided email.");
        repositoryMock.Verify(r => r.ExistsAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>()), Times.Once);
    }

    [Fact]
    public async Task Validate_WithSameUserEmail_ShouldReturnValid()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        Mock<IApplicationUserRepository> repositoryMock = new();
        repositoryMock
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>()))
            .ReturnsAsync(false); // Same user's email should not be considered duplicate

        EmailValidator validator = new(userId, repositoryMock.Object);
        string email = "user@example.com";

        // Act
        ValidationResult result = await validator.ValidateAsync(email);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
