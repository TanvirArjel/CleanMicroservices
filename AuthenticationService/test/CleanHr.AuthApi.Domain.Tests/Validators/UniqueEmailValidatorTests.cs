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

public class UniqueEmailValidatorTests
{
    [Fact]
    public async Task Validate_WithUniqueEmail_ShouldReturnValid()
    {
        // Arrange
        Mock<IApplicationUserRepository> repositoryMock = new();
        repositoryMock
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>()))
            .ReturnsAsync(false);

        UniqueEmailValidator validator = new(Guid.NewGuid(), repositoryMock.Object);
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

        UniqueEmailValidator validator = new(Guid.NewGuid(), repositoryMock.Object);
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

        UniqueEmailValidator validator = new(userId, repositoryMock.Object);
        string email = "user@example.com";

        // Act
        ValidationResult result = await validator.ValidateAsync(email);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
