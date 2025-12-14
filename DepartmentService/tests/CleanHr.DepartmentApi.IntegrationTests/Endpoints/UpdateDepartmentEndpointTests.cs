using System.Net;
using System.Net.Http.Json;
using CleanHr.DepartmentApi.IntegrationTests.Fixtures;
using CleanHr.DepartmentApi.IntegrationTests.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;

namespace CleanHr.DepartmentApi.IntegrationTests.Endpoints;

public class UpdateDepartmentEndpointTests : IClassFixture<DepartmentApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public UpdateDepartmentEndpointTests(DepartmentApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private static string GenerateValidName() => $"Dept{Guid.NewGuid().ToString()[..8]}";
    private static string GenerateValidDescription() => "This is a valid test description that meets the minimum length requirement.";

    public static TheoryData<string, string> InvalidNameTestData => new()
    {
        // { name, errorMessage }
        { null, "The Name is required." },
        { "", "The Name cannot be empty." },
        { "A", "The Name must be at least 2 characters." },
        { "ThisNameIsWayTooLongToBeValid", "The Name can't be more than 20 characters." }
    };

    public static TheoryData<string, string> InvalidDescriptionTestData => new()
    {
        // { description, errorMessage }
        { null, "The Description is required." },
        { "", "The Description cannot be empty." },
        { new string('A', 19), "The Description must be at least 20 characters." },
        { new string('A', 201), "The Description can't be more than 200 characters." }
    };

    public static TheoryData<string, string> UpdateDepartmentValidTestData => new()
    {
        // { name, newName }
        { new string('A', 2), new string('B', 2) }, // MinLength Name and NewName
        { new string('A', 20), new string('B', 20) }, // MaxLength Name and NewName
        { new string('A', 10), new string('A', 10) } // Name and NewName are same name
    };

    [Theory]
    [MemberData(nameof(UpdateDepartmentValidTestData))]
    public async Task Put_ReturnsOk_WhenValidRequest(string name, string newName)
    {
        // Arrange - First create a department to update
        var createRequest = new CreateDepartmentRequest
        {
            Name = name,
            Description = GenerateValidDescription()
        };
        var createResponse = await _client.PostAsJsonAsync(TestConstants.BaseUrl, createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var location = createResponse.Headers.Location!.ToString();
        var departmentId = Guid.Parse(location.Split('/').Last());

        var newDescription = "This is a fully updated description that meets the minimum length requirement.";
        var updateRequest = new UpdateDepartmentRequest
        {
            Id = departmentId,
            Name = newName,
            Description = newDescription
        };

        // Act
        var response = await _client.PutAsJsonAsync($"{TestConstants.BaseUrl}/{departmentId}?departmentId={departmentId}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType.MediaType.Should().Be("application/json");

        // Verify the update
        var getResponse = await _client.GetAsync($"{TestConstants.BaseUrl}/{departmentId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updatedDepartment = await getResponse.Content.ReadFromJsonAsync<DepartmentResponse>();
        updatedDepartment.Should().NotBeNull();
        updatedDepartment!.Name.Should().Be(newName);
        updatedDepartment.Description.Should().Be(newDescription);
    }

    [Fact]
    public async Task Put_ReturnsBadRequest_WhenIdMismatch()
    {
        // Arrange - First create a department
        var createRequest = new CreateDepartmentRequest
        {
            Name = GenerateValidName(),
            Description = GenerateValidDescription()
        };
        var createResponse = await _client.PostAsJsonAsync(TestConstants.BaseUrl, createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var location = createResponse.Headers.Location!.ToString();
        var departmentId = Guid.Parse(location.Split('/').Last());
        var differentId = Guid.NewGuid();

        var updateRequest = new UpdateDepartmentRequest
        {
            Id = differentId, // Different from route
            Name = GenerateValidName(),
            Description = GenerateValidDescription()
        };

        // Act
        var response = await _client.PutAsJsonAsync($"{TestConstants.BaseUrl}/{departmentId}?departmentId={departmentId}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ValidationProblemDetails problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problemDetails.Should().NotBeNull();
        problemDetails.Title.Should().Be("One or more validation errors occurred.");
        problemDetails.Errors.Should().ContainKey("Id");
        problemDetails.Errors["Id"].Should().Contain("The DepartmentId does not match with route value.");
    }

    [Fact]
    public async Task Put_ReturnsBadRequest_WhenDuplicateName()
    {
        // Arrange - Create two departments
        var firstRequest = new CreateDepartmentRequest
        {
            Name = GenerateValidName(),
            Description = GenerateValidDescription()
        };
        var firstResponse = await _client.PostAsJsonAsync(TestConstants.BaseUrl, firstRequest);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var secondName = GenerateValidName();
        var secondRequest = new CreateDepartmentRequest
        {
            Name = secondName,
            Description = GenerateValidDescription()
        };
        var secondResponse = await _client.PostAsJsonAsync(TestConstants.BaseUrl, secondRequest);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var firstLocation = firstResponse.Headers.Location!.ToString();
        var firstDepartmentId = Guid.Parse(firstLocation.Split('/').Last());

        // Try to update first department with second department's name
        var updateRequest = new UpdateDepartmentRequest
        {
            Id = firstDepartmentId,
            Name = secondName, // Using the name of another department
            Description = GenerateValidDescription()
        };

        // Act
        var response = await _client.PutAsJsonAsync($"{TestConstants.BaseUrl}/{firstDepartmentId}?departmentId={firstDepartmentId}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ValidationProblemDetails problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problemDetails.Should().NotBeNull();
        problemDetails!.Title.Should().Be("One or more validation errors occurred.");
        problemDetails.Errors.Should().ContainKey("Name");
        problemDetails.Errors["Name"].Should().Contain("The Name is already existent.");
    }

    [Theory]
    [MemberData(nameof(InvalidNameTestData))]
    public async Task Put_ReturnsBadRequest_WhenNameIsInvalid(string name, string errorMessage)
    {
        // Arrange - First create a department to update
        var createRequest = new CreateDepartmentRequest
        {
            Name = GenerateValidName(),
            Description = GenerateValidDescription()
        };
        var createResponse = await _client.PostAsJsonAsync(TestConstants.BaseUrl, createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var location = createResponse.Headers.Location!.ToString();
        var departmentId = Guid.Parse(location.Split('/').Last());

        var updateRequest = new UpdateDepartmentRequest
        {
            Id = departmentId,
            Name = name,
            Description = GenerateValidDescription()
        };

        // Act
        var response = await _client.PutAsJsonAsync($"{TestConstants.BaseUrl}/{departmentId}?departmentId={departmentId}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ValidationProblemDetails problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problemDetails.Should().NotBeNull();
        problemDetails!.Errors.Should().ContainKey("Name");
        problemDetails.Errors["Name"].Should().Contain(errorMessage);
    }

    [Theory]
    [MemberData(nameof(InvalidDescriptionTestData))]
    public async Task Put_ReturnsBadRequest_WhenDescriptionIsInvalid(string description, string errorMessage)
    {
        // Arrange - First create a department to update
        var createRequest = new CreateDepartmentRequest
        {
            Name = GenerateValidName(),
            Description = GenerateValidDescription()
        };
        var createResponse = await _client.PostAsJsonAsync(TestConstants.BaseUrl, createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var location = createResponse.Headers.Location!.ToString();
        var departmentId = Guid.Parse(location.Split('/').Last());

        var updateRequest = new UpdateDepartmentRequest
        {
            Id = departmentId,
            Name = GenerateValidName(),
            Description = description
        };

        // Act
        var response = await _client.PutAsJsonAsync($"{TestConstants.BaseUrl}/{departmentId}?departmentId={departmentId}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ValidationProblemDetails problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problemDetails.Should().NotBeNull();
        problemDetails!.Errors.Should().ContainKey("Description");
        problemDetails.Errors["Description"].Should().Contain(errorMessage);
    }
}
