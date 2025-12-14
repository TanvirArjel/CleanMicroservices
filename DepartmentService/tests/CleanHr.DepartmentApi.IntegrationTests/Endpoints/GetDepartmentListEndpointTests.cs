using System.Net;
using System.Net.Http.Json;
using CleanHr.DepartmentApi.IntegrationTests.Fixtures;
using CleanHr.DepartmentApi.IntegrationTests.Models;
using FluentAssertions;

namespace CleanHr.DepartmentApi.IntegrationTests.Endpoints;

public class GetDepartmentListEndpointTests : IClassFixture<DepartmentApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public GetDepartmentListEndpointTests(DepartmentApiWebApplicationFactory factory)
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
    public async Task Get_ReturnsOk_WhenDepartmentsExist()
    {
        // Arrange - Create a department first
        await CreateDepartmentAsync();

        // Act
        var response = await _client.GetAsync(TestConstants.BaseUrl);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var departments = await response.Content.ReadFromJsonAsync<List<DepartmentResponse>>();
        departments.Should().NotBeNull();
        departments.Should().HaveCountGreaterThan(0);
        departments.Should().AllSatisfy(d =>
        {
            d.Id.Should().NotBeEmpty();
            d.Name.Should().NotBeNullOrEmpty();
        });
    }

    [Fact]
    public async Task Get_ReturnsCreatedDepartments()
    {
        // Arrange - Create some departments
        var (_, name1) = await CreateDepartmentAsync();
        var (_, name2) = await CreateDepartmentAsync();
        var (_, name3) = await CreateDepartmentAsync();

        // Act
        var response = await _client.GetAsync(TestConstants.BaseUrl);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var departments = await response.Content.ReadFromJsonAsync<List<DepartmentResponse>>();
        departments.Should().NotBeNull();
        departments.Should().Contain(d => d.Name == name1);
        departments.Should().Contain(d => d.Name == name2);
        departments.Should().Contain(d => d.Name == name3);
    }
}
