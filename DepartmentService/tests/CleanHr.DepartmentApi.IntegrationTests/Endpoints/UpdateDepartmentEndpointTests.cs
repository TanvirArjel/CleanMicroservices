using System.Net;
using System.Net.Http.Json;
using CleanHr.DepartmentApi.IntegrationTests.Fixtures;
using CleanHr.DepartmentApi.IntegrationTests.Models;
using FluentAssertions;

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

    [Fact]
    public async Task Put_ReturnsOk_WhenValidRequest()
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
            Description = GenerateValidDescription()
        };

        // Act
        var response = await _client.PutAsJsonAsync($"{TestConstants.BaseUrl}/{departmentId}?departmentId={departmentId}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
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
    }

    [Fact]
    public async Task Put_ReturnsBadRequest_WhenNameIsEmpty()
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
            Name = "",
            Description = GenerateValidDescription()
        };

        // Act
        var response = await _client.PutAsJsonAsync($"{TestConstants.BaseUrl}/{departmentId}?departmentId={departmentId}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Put_ReturnsBadRequest_WhenNameIsNull()
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
            Name = null,
            Description = GenerateValidDescription()
        };

        // Act
        var response = await _client.PutAsJsonAsync($"{TestConstants.BaseUrl}/{departmentId}?departmentId={departmentId}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Put_UpdatesDepartment_Successfully()
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

        var newName = GenerateValidName();
        var newDescription = "This is a fully updated description that meets the minimum length requirement.";
        var updateRequest = new UpdateDepartmentRequest
        {
            Id = departmentId,
            Name = newName,
            Description = newDescription
        };

        // Act
        var updateResponse = await _client.PutAsJsonAsync($"{TestConstants.BaseUrl}/{departmentId}?departmentId={departmentId}", updateRequest);

        // Assert
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify the update
        var getResponse = await _client.GetAsync($"{TestConstants.BaseUrl}/{departmentId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updatedDepartment = await getResponse.Content.ReadFromJsonAsync<DepartmentResponse>();
        updatedDepartment.Should().NotBeNull();
        updatedDepartment!.Name.Should().Be(newName);
        updatedDepartment.Description.Should().Be(newDescription);
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
    }

    [Fact]
    public async Task Put_ReturnsOk_WhenUpdatingWithSameName()
    {
        // Arrange - Create a department and update it with the same name
        var originalName = GenerateValidName();
        var createRequest = new CreateDepartmentRequest
        {
            Name = originalName,
            Description = GenerateValidDescription()
        };
        var createResponse = await _client.PostAsJsonAsync(TestConstants.BaseUrl, createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var location = createResponse.Headers.Location!.ToString();
        var departmentId = Guid.Parse(location.Split('/').Last());

        var updateRequest = new UpdateDepartmentRequest
        {
            Id = departmentId,
            Name = originalName, // Same name
            Description = "Updated description only - this description meets the minimum length requirement."
        };

        // Act
        var response = await _client.PutAsJsonAsync($"{TestConstants.BaseUrl}/{departmentId}?departmentId={departmentId}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Put_ReturnsUpdatedDepartment_InResponseBody()
    {
        // Arrange
        var createRequest = new CreateDepartmentRequest
        {
            Name = GenerateValidName(),
            Description = GenerateValidDescription()
        };
        var createResponse = await _client.PostAsJsonAsync(TestConstants.BaseUrl, createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var location = createResponse.Headers.Location!.ToString();
        var departmentId = Guid.Parse(location.Split('/').Last());

        var newName = GenerateValidName();
        var updateRequest = new UpdateDepartmentRequest
        {
            Id = departmentId,
            Name = newName,
            Description = "Updated for response test - this description meets the minimum length requirement."
        };

        // Act
        var response = await _client.PutAsJsonAsync($"{TestConstants.BaseUrl}/{departmentId}?departmentId={departmentId}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updatedDepartment = await response.Content.ReadFromJsonAsync<UpdateDepartmentRequest>();
        updatedDepartment.Should().NotBeNull();
        updatedDepartment!.Name.Should().Be(newName);
    }
}
