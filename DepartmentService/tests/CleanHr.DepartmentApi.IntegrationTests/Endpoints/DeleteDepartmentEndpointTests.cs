using System.Net;
using System.Net.Http.Json;
using CleanHr.DepartmentApi.IntegrationTests.Fixtures;
using CleanHr.DepartmentApi.IntegrationTests.Models;
using FluentAssertions;

namespace CleanHr.DepartmentApi.IntegrationTests.Endpoints;

public class DeleteDepartmentEndpointTests : IClassFixture<DepartmentApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public DeleteDepartmentEndpointTests(DepartmentApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private static string GenerateValidName() => $"Dept{Guid.NewGuid().ToString()[..8]}";
    private static string GenerateValidDescription() => "This is a valid test description that meets the minimum length requirement.";

    [Fact]
    public async Task Delete_ReturnsNoContent_WhenDepartmentExists()
    {
        // Arrange - First create a department to delete
        var createRequest = new CreateDepartmentRequest
        {
            Name = GenerateValidName(),
            Description = GenerateValidDescription()
        };
        var createResponse = await _client.PostAsJsonAsync(TestConstants.BaseUrl, createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var location = createResponse.Headers.Location.ToString();
        var departmentId = Guid.Parse(location.Split('/').Last());

        // Act
        var response = await _client.DeleteAsync($"{TestConstants.BaseUrl}/{departmentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Assert - Verify it's no longer retrievable
        var getAfterDelete = await _client.GetAsync($"{TestConstants.BaseUrl}/{departmentId}");
        getAfterDelete.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_ReturnsBadRequest_WhenIdIsEmpty()
    {
        // Arrange
        var emptyGuid = Guid.Empty;

        // Act
        var response = await _client.DeleteAsync($"{TestConstants.BaseUrl}/{emptyGuid}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Delete_MultipleSequentialDeletes_WorkCorrectly()
    {
        // Arrange - Create two departments
        var dept1 = new CreateDepartmentRequest
        {
            Name = GenerateValidName(),
            Description = GenerateValidDescription()
        };
        var dept2 = new CreateDepartmentRequest
        {
            Name = GenerateValidName(),
            Description = GenerateValidDescription()
        };

        var createResponse1 = await _client.PostAsJsonAsync(TestConstants.BaseUrl, dept1);
        var createResponse2 = await _client.PostAsJsonAsync(TestConstants.BaseUrl, dept2);

        var id1 = Guid.Parse(createResponse1.Headers.Location?.ToString().Split('/').Last());
        var id2 = Guid.Parse(createResponse2.Headers.Location?.ToString().Split('/').Last());

        // Act
        var deleteResponse1 = await _client.DeleteAsync($"{TestConstants.BaseUrl}/{id1}");
        var deleteResponse2 = await _client.DeleteAsync($"{TestConstants.BaseUrl}/{id2}");

        // Assert
        deleteResponse1.StatusCode.Should().Be(HttpStatusCode.NoContent);
        deleteResponse2.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify both are deleted
        var get1 = await _client.GetAsync($"{TestConstants.BaseUrl}/{id1}");
        var get2 = await _client.GetAsync($"{TestConstants.BaseUrl}/{id2}");

        get1.StatusCode.Should().Be(HttpStatusCode.NotFound);
        get2.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_DoesNotAffectOtherDepartments()
    {
        // Arrange - Create two departments
        var deptToDeleteName = GenerateValidName();
        var deptToKeepName = GenerateValidName();

        var deptToDelete = new CreateDepartmentRequest
        {
            Name = deptToDeleteName,
            Description = GenerateValidDescription()
        };
        var deptToKeep = new CreateDepartmentRequest
        {
            Name = deptToKeepName,
            Description = GenerateValidDescription()
        };

        var createResponse1 = await _client.PostAsJsonAsync(TestConstants.BaseUrl, deptToDelete);
        var createResponse2 = await _client.PostAsJsonAsync(TestConstants.BaseUrl, deptToKeep);

        var idToDelete = Guid.Parse(createResponse1.Headers.Location!.ToString().Split('/').Last());
        var idToKeep = Guid.Parse(createResponse2.Headers.Location!.ToString().Split('/').Last());

        // Act
        var deleteResponse = await _client.DeleteAsync($"{TestConstants.BaseUrl}/{idToDelete}");

        // Assert
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify the other department still exists
        var getKept = await _client.GetAsync($"{TestConstants.BaseUrl}/{idToKeep}");
        getKept.StatusCode.Should().Be(HttpStatusCode.OK);

        var keptDepartment = await getKept.Content.ReadFromJsonAsync<DepartmentResponse>();
        keptDepartment.Should().NotBeNull();
        keptDepartment!.Name.Should().Be(deptToKeepName);
    }
}
