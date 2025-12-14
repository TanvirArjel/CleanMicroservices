using System.Net;
using System.Net.Http.Json;
using CleanHr.DepartmentApi.IntegrationTests.Fixtures;
using CleanHr.DepartmentApi.IntegrationTests.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;

namespace CleanHr.DepartmentApi.IntegrationTests.Endpoints;

public class CreateDepartmentEndpointTests : IClassFixture<DepartmentApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CreateDepartmentEndpointTests(DepartmentApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

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

    public static TheoryData<string, string> CreateDepartmentValidTestData => new()
    {
        // { name, description }
        { new string('A', 2), new string('B', 20) }, // MinLength Name and Description
        { new string('A', 20), new string('B', 200) } // MaxLength Name and Description
    };

    [Theory]
    [MemberData(nameof(CreateDepartmentValidTestData))]
    public async Task Post_ReturnsCreated_WithValidData(string name, string description)
    {
        // Arrange
        var request = new CreateDepartmentRequest
        {
            Name = name,  // Minimum 2 characters
            Description = description
        };

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync(TestConstants.BaseUrl, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain("/api/v1/departments/");

        CreateDepartmentRequest createdDepartment = await response.Content.ReadFromJsonAsync<CreateDepartmentRequest>();
        createdDepartment.Should().NotBeNull();
        createdDepartment!.Name.Should().Be(request.Name);

        // Get the location and retrieve the created department
        Uri location = response.Headers.Location;
        location.Should().NotBeNull();

        HttpResponseMessage getResponse = await _client.GetAsync(location);
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        DepartmentResponse retrievedDepartment = await getResponse.Content.ReadFromJsonAsync<DepartmentResponse>();
        retrievedDepartment.Should().NotBeNull();
        retrievedDepartment!.Name.Should().Be(request.Name);
    }

    [Theory]
    [MemberData(nameof(InvalidNameTestData))]
    public async Task Post_ReturnsBadRequest_WhenNameIsInvalid(string name, string errorMessage)
    {
        // Arrange
        var request = new CreateDepartmentRequest
        {
            Name = name,
            Description = new string('B', 50) // valid description
        };

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync(TestConstants.BaseUrl, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ValidationProblemDetails problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problemDetails.Should().NotBeNull();
        problemDetails!.Errors.Should().ContainKey("Name");
        problemDetails.Errors["Name"].Should().Contain(errorMessage);
    }

    [Theory]
    [MemberData(nameof(InvalidDescriptionTestData))]
    public async Task Post_ReturnsBadRequest_WhenDescriptionIsInvalid(string description, string errorMessage)
    {
        // Arrange
        var request = new CreateDepartmentRequest
        {
            Name = new string('A', 10), // valid name
            Description = description
        };

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync(TestConstants.BaseUrl, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ValidationProblemDetails problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problemDetails.Should().NotBeNull();
        problemDetails.Errors.Should().ContainKey("Description");
        problemDetails.Errors["Description"].Should().Contain(errorMessage);
    }

    [Fact]
    public async Task Post_ReturnsBadRequest_WhenDuplicateName()
    {
        // Arrange - First create a department
        var uniqueName = new string('A', 10);
        var firstRequest = new CreateDepartmentRequest
        {
            Name = uniqueName,
            Description = new string('B', 50)
        };
        await _client.PostAsJsonAsync(TestConstants.BaseUrl, firstRequest);

        // Now try to create another with the same name
        var duplicateRequest = new CreateDepartmentRequest
        {
            Name = uniqueName,
            Description = new string('B', 50)
        };

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync(TestConstants.BaseUrl, duplicateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ValidationProblemDetails problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problemDetails.Should().NotBeNull();
        problemDetails!.Errors.Should().ContainKey("Name");
        problemDetails.Errors["Name"].Should().Contain("The Name is already existent.");
    }
}
