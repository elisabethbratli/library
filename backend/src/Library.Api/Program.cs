using Library.Api.Auth;
using Library.Api.Books;
using Library.Api.Data;
using Library.Api.Reviews;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ConnectionStrings__Library is set on the container in Scaleway.
builder.Services.AddDbContext<LibraryDb>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("Library")
        ?? "Host=localhost;Database=library;Username=library;Password=library"));
builder.AddPasswordAuth();

var app = builder.Build();
var password = app.Configuration["Auth:Password"]!;

using (var scope = app.Services.CreateScope())
    await scope.ServiceProvider.GetRequiredService<LibraryDb>().Database.MigrateAsync();

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/healthz", () => Results.Ok("ok")).AllowAnonymous();

var api = app.MapGroup("/api");
api.MapAuthEndpoints(password);
api.MapBookEndpoints();
api.MapReviewEndpoints();

app.Run();

public partial class Program; // for WebApplicationFactory in tests
