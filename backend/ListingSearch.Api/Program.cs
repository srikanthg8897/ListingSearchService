using ListingSearch.Api.Repositories;
using ListingSearch.Api.Services;
using ListingSearch.Api.Validation;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalFrontend", policy =>
        policy.WithOrigins("http://localhost:5173", "http://localhost:8080")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
builder.Services.AddSingleton<IListingRepository>(_ =>
    new ListingRepository(Path.Combine(AppContext.BaseDirectory, "Data", "sample_listings.json")));
builder.Services.AddSingleton<SearchRequestValidator>();
builder.Services.AddSingleton<RelevanceScoringService>();
builder.Services.AddSingleton<ListingSearchService>();

var app = builder.Build();

app.UseCors("LocalFrontend");
app.MapControllers();

app.Run();

/// <summary>
/// Exposes the entry point to the integration-test host.
/// </summary>
public partial class Program
{
}
