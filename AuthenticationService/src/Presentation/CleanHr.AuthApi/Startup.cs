using System.IO.Compression;
using CleanHr.AuthApi.Extensions;
using CleanHr.AuthApi.Filters;
using CleanHr.AuthApi.Utilities;
using CleanHr.AuthApi.Application.Commands;
using CleanHr.AuthApi.Application.Infrastructures;
using CleanHr.AuthApi.Infrastructure.Services;
using CleanHr.AuthApi.Persistence.Cache;
using CleanHr.AuthApi.Persistence.RelationalDB.Extensions;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.ResponseCompression;
using OpenTelemetry.Metrics;
using Swashbuckle.AspNetCore.SwaggerUI;
using TanvirArjel.Extensions.Microsoft.DependencyInjection;

namespace CleanHr.AuthApi;

internal static class Startup
{
    private const string _myAllowSpecificOrigins = "_myAllowSpecificOrigins";

    private static bool InDocker => Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";

    // This method gets called by the runtime. Use this method to add services to the container.
    public static void ConfigureServices(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        IServiceCollection services = builder.Services;
        string connectionString = builder.GetDbConnectionString();

        services.AddAllHealthChecks(connectionString);
        services.AddHostedService<ConfigurationLoadingBackgroundService>();

        services.AddCors(options =>
        {
            options.AddPolicy(
                name: _myAllowSpecificOrigins,
                builder =>
                {
                    builder.WithOrigins("http://localhost:5200", "https://localhost:5201")
                    .AllowAnyHeader()
                    .AllowAnyMethod();
                });
        });

        ////services.AddCors();
        services.AddFluentValidation();

        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
        });

        services.Configure<GzipCompressionProviderOptions>(options =>
        {
            options.Level = CompressionLevel.Fastest;
        });

        services.Configure<BrotliCompressionProviderOptions>(options =>
        {
            options.Level = CompressionLevel.Fastest;
        });

        services.AddRelationalDbContext(connectionString);

        string sendGridApiKey = "yourSendGridKey";
        services.AddSendGrid(sendGridApiKey);

        services.AddCaching();

        services.AddHttpContextAccessor();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<LoginUserCommand>());

        services.AddServicesOfAllTypes("CleanHr");
        services.AddControllersWithViews(options =>
        {
            options.Filters.Add<BadRequestResultFilter>();
            options.Filters.Add<ExceptionHandlerFilter>();
            options.Conventions.Add(new RouteTokenTransformerConvention(new SlugifyParameterTransformer()));
        }).ConfigureApiBehaviorOptions(options =>
        {
            options.SuppressModelStateInvalidFilter = false;
        })
        .AddApplicationPart(typeof(Startup).Assembly);

        services.AddEndpointsApiExplorer();

        services.AddSwaggerGeneration("Clean HR Auth API", "CleanHr.AuthApi");

        JwtConfig jwtConfig = new("SampleIdentity.com", "SampleIdentitySecretKeyNeedsToBeLongEnough", 86400);
        services.AddJwtAuthentication(jwtConfig);

        services.AddJwtTokenGenerator(jwtConfig);

        services.AddExternalLogins(builder.Configuration);
    }

    public static async Task ConfigureMiddlewaresAsync(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.ApplyDatabaseMigrations();

        await app.SeedDatabaseAsync();

        app.Use((context, next) =>
        {
            context.Request.EnableBuffering();
            return next();
        });

        // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.),
        // specifying the Swagger JSON endpoint.
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.DocExpansion(DocExpansion.None);

            IApiVersionDescriptionProvider provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();

            // build a swagger endpoint for each discovered API version.
            foreach (ApiVersionDescription description in provider.ApiVersionDescriptions)
            {
                options.RoutePrefix = "swagger";
                options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json", description.GroupName.ToUpperInvariant());
            }
        });

        app.UseResponseCompression();

        // If you are using http url then don't enable https redirection.
        ////app.UseHttpsRedirection();

        app.AddHealthCheckEndpoints();

        app.UseRouting();

        app.UseCors(_myAllowSpecificOrigins);
        ////app.UseCors(options => options.SetIsOriginAllowed(x => _ = true).AllowAnyMethod().AllowAnyHeader().AllowCredentials());

        // OpenTelemetry Prometheus metrics endpoint (must be before authentication to allow anonymous access)
        app.MapPrometheusScrapingEndpoint();

        app.UseAuthentication();
        app.UseAuthorization();

        // Add controller routes
        app.MapControllers();
    }
}
