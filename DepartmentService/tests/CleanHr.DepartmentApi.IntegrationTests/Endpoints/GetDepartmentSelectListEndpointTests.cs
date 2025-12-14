using System.Net;
using System.Net.Http.Json;
using CleanHr.DepartmentApi.IntegrationTests.Fixtures;
using CleanHr.DepartmentApi.IntegrationTests.Models;
using FluentAssertions;

namespace CleanHr.DepartmentApi.IntegrationTests.Endpoints;

public class GetDepartmentSelectListEndpointTests : IClassFixture<DepartmentApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public GetDepartmentSelectListEndpointTests(DepartmentApiWebApplicationFactory factory)
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
    public async Task Get_ReturnsOk_WhenNoSelectedDepartment()
    {
        // Act
        var response = await _client.GetAsync($"{TestConstants.BaseUrl}/select-list");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_ReturnsSelectList_WithDepartments()
    {
        // Arrange - Create a department first
        await CreateDepartmentAsync();

        // Act
        var response = await _client.GetAsync($"{TestConstants.BaseUrl}/select-list");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var selectListItems = await response.Content.ReadFromJsonAsync<List<SelectListItem>>();
        selectListItems.Should().NotBeNull();
        selectListItems.Should().HaveCountGreaterThan(0);

        selectListItems.Should().AllSatisfy(item =>
       {
           item.Text.Should().NotBeNullOrEmpty();
           item.Value.Should().NotBeNullOrEmpty();
       });
    }

    [Fact]
    public async Task Get_ReturnsOk_WithValidSelectedDepartment()
    {
        // Arrange - Create a department first
        var (departmentId, _) = await CreateDepartmentAsync();

        // Act
        var response = await _client.GetAsync($"{TestConstants.BaseUrl}/select-list?selectedDepartment={departmentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var selectListItems = await response.Content.ReadFromJsonAsync<List<SelectListItem>>();
        selectListItems.Should().NotBeNull();
        selectListItems.Should().Contain(item => item.Selected && item.Value == departmentId.ToString());
    }

    [Fact]
    public async Task Get_ReturnsBadRequest_WhenSelectedDepartmentIsEmptyGuid()
    {
        // Arrange
        var emptyGuid = Guid.Empty;

        // Act
        var response = await _client.GetAsync($"{TestConstants.BaseUrl}/select-list?selectedDepartment={emptyGuid}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Get_ReturnsSelectList_ContainingCreatedDepartments()
    {
        // Arrange - Create some departments
        var (_, name1) = await CreateDepartmentAsync();
        var (_, name2) = await CreateDepartmentAsync();
        var (_, name3) = await CreateDepartmentAsync();

        // Act
        var response = await _client.GetAsync($"{TestConstants.BaseUrl}/select-list");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var selectListItems = await response.Content.ReadFromJsonAsync<List<SelectListItem>>();
        selectListItems.Should().NotBeNull();
        selectListItems.Should().Contain(item => item.Text == name1);
        selectListItems.Should().Contain(item => item.Text == name2);
        selectListItems.Should().Contain(item => item.Text == name3);
    }
}
