using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using CleanHr.EmployeeApi.Domain.Aggregates;

namespace CleanHr.EmployeeApi.Infrastructure.Services;

public sealed class DepartmentServiceClient : IDepartmentServiceClient
{
    private readonly HttpClient _httpClient;

    public DepartmentServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<bool> IsDepartmentExistentAsync(Guid departmentId, CancellationToken cancellationToken = default)
    {
        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync($"/api/departments/{departmentId}/exists", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                bool? exists = await response.Content.ReadFromJsonAsync<bool>(cancellationToken);
                return exists ?? false;
            }

            return false;
        }
        catch
        {
            // Log the exception in production
            return false;
        }
    }
}
