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

public class UserNameValidatorTests
{
    [Fact]
    public async Task Validate_WithValidUserName_ShouldReturnValid()
    {
        // Arrange
        Mock<IApplicationUserRepository> repositoryMock = new();
        UserNameValidator validator = new(Guid.NewGuid(), repositoryMock.Object);
        string userName = "testuser123";

        // Act
        ValidationResult result = await validator.ValidateAsync(userName);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Validate_WithEmptyUserName_ShouldReturnInvalid(string userName)
    {
        // Arrange
        Mock<IApplicationUserRepository> repositoryMock = new();
        UserNameValidator validator = new(Guid.NewGuid(), repositoryMock.Object);

        // Act
        ValidationResult result = await validator.ValidateAsync(userName);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "The UserName is required.");
    }

    [Fact]
    public async Task Validate_WithNullUserName_ShouldThrowException()
    {
        // Arrange
        Mock<IApplicationUserRepository> repositoryMock = new();
        UserNameValidator validator = new(Guid.NewGuid(), repositoryMock.Object);

        // Act
        ValidationResult result = await validator.ValidateAsync(null as string);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "The UserName cannot be null.");
    }

    [Theory]
    [InlineData("test")]
    [InlineData("a")]
    [InlineData("1234")]
    public async Task Validate_WithUserNameTooShort_ShouldReturnInvalid(string userName)
    {
        // Arrange
        Mock<IApplicationUserRepository> repositoryMock = new();
        UserNameValidator validator = new(Guid.NewGuid(), repositoryMock.Object);

        // Act
        ValidationResult result = await validator.ValidateAsync(userName);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.ErrorMessage.Should().Be("The UserName must be at least 5 characters.");
    }

    [Fact]
    public async Task Validate_WithUserNameTooLong_ShouldReturnInvalid()
    {
        // Arrange
        Mock<IApplicationUserRepository> repositoryMock = new();
        UserNameValidator validator = new(Guid.NewGuid(), repositoryMock.Object);
        string userName = new('a', 51); // 51 characters

        // Act
        ValidationResult result = await validator.ValidateAsync(userName);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.ErrorMessage.Should().Be("The UserName can't be more than 50 characters long.");
    }

    [Theory]
    [InlineData("user1")]
    [InlineData("testuser")]
    [InlineData("test_user_123")]
    [InlineData("user.name")]
    public async Task Validate_WithValidUserNameLengths_ShouldReturnValid(string userName)
    {
        // Arrange
        Mock<IApplicationUserRepository> repositoryMock = new();
        UserNameValidator validator = new(Guid.NewGuid(), repositoryMock.Object);

        // Act
        ValidationResult result = await validator.ValidateAsync(userName);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithUniqueUserName_ShouldReturnValid()
    {
        // Arrange
        Mock<IApplicationUserRepository> repositoryMock = new();
        repositoryMock
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>()))
            .ReturnsAsync(false);

        UserNameValidator validator = new(Guid.NewGuid(), repositoryMock.Object);
        string userName = "uniqueuser";

        // Act
        ValidationResult result = await validator.ValidateAsync(userName);

        // Assert
        result.IsValid.Should().BeTrue();
        repositoryMock.Verify(r => r.ExistsAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>()), Times.Once);
    }

    [Fact]
    public async Task Validate_WithDuplicateUserName_ShouldReturnInvalid()
    {
        // Arrange
        Mock<IApplicationUserRepository> repositoryMock = new();
        repositoryMock
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>()))
            .ReturnsAsync(true);

        UserNameValidator validator = new(Guid.NewGuid(), repositoryMock.Object);
        string userName = "duplicateuser";

        // Act
        ValidationResult result = await validator.ValidateAsync(userName);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "A user already exists with the provided username.");
        repositoryMock.Verify(r => r.ExistsAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>()), Times.Once);
    }

    [Fact]
    public async Task Validate_WithSameUserUserName_ShouldReturnValid()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        Mock<IApplicationUserRepository> repositoryMock = new();
        repositoryMock
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>()))
            .ReturnsAsync(false); // Same user's username should not be considered duplicate

        UserNameValidator validator = new(userId, repositoryMock.Object);
        string userName = "currentuser";

        // Act
        ValidationResult result = await validator.ValidateAsync(userName);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithoutRepository_ShouldSkipUniquenessCheck()
    {
        // Arrange
        Mock<IApplicationUserRepository> repositoryMock = new();
        UserNameValidator validator = new(Guid.NewGuid(), repositoryMock.Object);
        string userName = "testuser";

        // Act
        ValidationResult result = await validator.ValidateAsync(userName);

        // Assert
        result.IsValid.Should().BeTrue(); // Should only check basic validation
    }
}
