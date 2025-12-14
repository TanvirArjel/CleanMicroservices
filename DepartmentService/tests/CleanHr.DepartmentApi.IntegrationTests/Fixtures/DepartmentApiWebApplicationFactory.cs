using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using CleanHr.DepartmentApi.Persistence.RelationalDB;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace CleanHr.DepartmentApi.IntegrationTests.Fixtures;

public class DepartmentApiWebApplicationFactory
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        builder.ConfigureTestServices(services =>
        {
            // Find and remove ALL DbContext-related registrations
            var dbContextDescriptors = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<CleanHrDbContext>) ||
                            d.ServiceType == typeof(DbContextOptions) ||
                            d.ServiceType.FullName?.Contains("CleanHrDbContext") == true)
                .ToList();

            foreach (var descriptor in dbContextDescriptors)
            {
                services.Remove(descriptor);
            }

            // Create a unique database name for this test instance
            var dbName = $"TestDb_{Guid.NewGuid()}";

            // Build a new internal service provider for in-memory database
            var inMemoryServiceProvider = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();

            // Add in-memory database with a fresh internal service provider
            services.AddDbContext<CleanHrDbContext>((sp, options) =>
            {
                options.UseInMemoryDatabase(dbName);
                // Use a new internal service provider to avoid conflicts with SQL Server services
                options.UseInternalServiceProvider(inMemoryServiceProvider);
            });
        });
    }

    protected override void ConfigureClient(HttpClient client)
    {
        base.ConfigureClient(client);

        // Add JWT token to all requests
        var token = TestJwtTokenGenerator.GenerateToken();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }
}

public static class TestJwtTokenGenerator
{
    // These values must match the application's JWT configuration in Startup.cs
    public const string SecretKey = "SampleIdentitySecretKeyNeedsToBeLongEnough";
    public const string Issuer = "SampleIdentity.com";
    public const string Audience = "SampleIdentity.com"; // Same as Issuer in the app config

    public static string GenerateToken()
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(SecretKey));

        var credentials = new SigningCredentials(
            key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, "test-user-id"),
            new Claim(JwtRegisteredClaimNames.Email, "test@example.com"),
            new Claim(ClaimTypes.Name, "Test User"),
            new Claim(ClaimTypes.Role, "Admin")
        };

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
