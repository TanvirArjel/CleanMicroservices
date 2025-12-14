using System.Net;
using System.Net.Http.Json;
using CleanHr.DepartmentApi.IntegrationTests.Fixtures;
using CleanHr.DepartmentApi.IntegrationTests.Models;
using FluentAssertions;

namespace CleanHr.DepartmentApi.IntegrationTests.Endpoints;

public class GetDepartmentByIdEndpointTests : IClassFixture<DepartmentApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public GetDepartmentByIdEndpointTests(DepartmentApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private static string GenerateValidName() => $"Dept{Guid.NewGuid().ToString()[..8]}";
    private static string GenerateValidDescription() => "This is a valid test description that meets the minimum length requirement.";

    private async Task<(Guid Id, string Name)> CreateDepartmentAsync()
    {
        var name = GenerateValidName();
        var createRequest = new CreateDepartmentRequest
        {
            Name = name,
            Description = GenerateValidDescription()
        };
        var createResponse = await _client.PostAsJsonAsync(TestConstants.BaseUrl, createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var location = createResponse.Headers.Location!.ToString();
        var departmentId = Guid.Parse(location.Split('/').Last());
        return (departmentId, name);
    }

    [Fact]
    public async Task GetDepartment_ReturnsOk_WhenDepartmentExists()
    {
        // Arrange - Create a department first
        var (departmentId, name) = await CreateDepartmentAsync();

        // Act
        var response = await _client.GetAsync($"{TestConstants.BaseUrl}/{departmentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var department = await response.Content.ReadFromJsonAsync<DepartmentResponse>();
        department.Should().NotBeNull();
        department.Id.Should().Be(departmentId);
        department.Name.Should().Be(name);
        department.Description.Should().NotBeNullOrEmpty();
        department.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetDepartment_ReturnsNotFound_WhenDepartmentDoesNotExist()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"{TestConstants.BaseUrl}/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetDepartment_ReturnsBadRequest_WhenIdIsEmpty()
    {
        // Arrange
        var emptyGuid = Guid.Empty;

        // Act
        var response = await _client.GetAsync($"{TestConstants.BaseUrl}/{emptyGuid}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
