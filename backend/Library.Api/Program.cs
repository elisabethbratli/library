using Library.Api.Data;
using Library.Api.Models;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Connection string: ConnectionStrings__Library env var (set by the container in Scaleway)
builder.Services.AddDbContext<LibraryDb>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("Library")
        ?? "Host=localhost;Database=library;Username=library;Password=library"));

var origins = builder.Configuration["AllowedOrigins"]?.Split(',', StringSplitOptions.RemoveEmptyEntries);
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
{
    if (origins is { Length: > 0 }) p.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
    else p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
}));

// --- Simple shared-password auth: POST /api/login sets an HttpOnly session cookie ---
var password = builder.Configuration["Auth:Password"];
if (string.IsNullOrEmpty(password))
    throw new InvalidOperationException("Auth__Password must be set");
var passwordHash = SHA256.HashData(Encoding.UTF8.GetBytes(password));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.Cookie.Name = "library_session";
        o.Cookie.HttpOnly = true;
        o.Cookie.SameSite = SameSiteMode.Lax;
        o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        o.ExpireTimeSpan = TimeSpan.FromDays(30);
        o.SlidingExpiration = true;
        // APIs should answer 401/403, not redirect to a login page
        o.Events.OnRedirectToLogin = c => { c.Response.StatusCode = 401; return Task.CompletedTask; };
        o.Events.OnRedirectToAccessDenied = c => { c.Response.StatusCode = 403; return Task.CompletedTask; };
    });
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser().Build());
// Brute-force protection: login attempts are limited globally (the API only sees the proxy's address)
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = 429;
    o.AddFixedWindowLimiter("login", l => { l.PermitLimit = 10; l.Window = TimeSpan.FromMinutes(1); l.QueueLimit = 0; });
});

var app = builder.Build();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// Create schema on startup. For a small home app this is enough; switch to EF migrations if the schema grows.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LibraryDb>();
    await db.Database.EnsureCreatedAsync();
}

app.MapGet("/healthz", () => Results.Ok("ok")).AllowAnonymous();

var api = app.MapGroup("/api");

api.MapPost("/login", async (LoginInput i, HttpContext ctx) =>
{
    var given = SHA256.HashData(Encoding.UTF8.GetBytes(i.Password ?? ""));
    if (!CryptographicOperations.FixedTimeEquals(given, passwordHash))
        return Results.Json(new { error = "Wrong password" }, statusCode: 401);
    var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [new Claim(ClaimTypes.Name, "owner")], CookieAuthenticationDefaults.AuthenticationScheme));
    await ctx.SignInAsync(principal, new AuthenticationProperties { IsPersistent = true });
    return Results.NoContent();
}).AllowAnonymous().RequireRateLimiting("login");

api.MapPost("/logout", async (HttpContext ctx) =>
{
    await ctx.SignOutAsync();
    return Results.NoContent();
}).AllowAnonymous();

api.MapGet("/me", () => Results.NoContent()); // 204 if logged in, 401 otherwise

static BookDto ToDto(Book b) => new(b.Id, b.Title, b.Author, b.Isbn, b.PublishedYear, b.Description, b.CoverUrl,
    b.AddedAt, b.Reviews.Count == 0 ? null : b.Reviews.Average(r => r.Rating), b.Reviews.Count);

static string? Validate(BookInput i) =>
    string.IsNullOrWhiteSpace(i.Title) ? "Title is required" :
    string.IsNullOrWhiteSpace(i.Author) ? "Author is required" : null;

api.MapGet("/books", async (string? q, LibraryDb db) =>
{
    var query = db.Books.Include(b => b.Reviews).AsQueryable();
    if (!string.IsNullOrWhiteSpace(q))
    {
        var term = $"%{q.Trim()}%";
        query = query.Where(b => EF.Functions.ILike(b.Title, term) || EF.Functions.ILike(b.Author, term));
    }
    var books = await query.OrderBy(b => b.Title).ToListAsync();
    return books.Select(ToDto);
});

api.MapGet("/books/{id:int}", async (int id, LibraryDb db) =>
{
    var b = await db.Books.Include(x => x.Reviews).FirstOrDefaultAsync(x => x.Id == id);
    return b is null ? Results.NotFound()
        : Results.Ok(new BookDetailDto(ToDto(b),
            b.Reviews.OrderByDescending(r => r.CreatedAt).Select(r => new ReviewDto(r.Id, r.Rating, r.Text, r.CreatedAt)).ToList()));
});

api.MapPost("/books", async (BookInput i, LibraryDb db) =>
{
    if (Validate(i) is { } err) return Results.BadRequest(new { error = err });
    var b = new Book
    {
        Title = i.Title.Trim(), Author = i.Author.Trim(), Isbn = i.Isbn?.Trim(),
        PublishedYear = i.PublishedYear, Description = i.Description, CoverUrl = i.CoverUrl
    };
    db.Books.Add(b);
    await db.SaveChangesAsync();
    return Results.Created($"/api/books/{b.Id}", ToDto(b));
});

api.MapPut("/books/{id:int}", async (int id, BookInput i, LibraryDb db) =>
{
    if (Validate(i) is { } err) return Results.BadRequest(new { error = err });
    var b = await db.Books.Include(x => x.Reviews).FirstOrDefaultAsync(x => x.Id == id);
    if (b is null) return Results.NotFound();
    b.Title = i.Title.Trim(); b.Author = i.Author.Trim(); b.Isbn = i.Isbn?.Trim();
    b.PublishedYear = i.PublishedYear; b.Description = i.Description; b.CoverUrl = i.CoverUrl;
    await db.SaveChangesAsync();
    return Results.Ok(ToDto(b));
});

api.MapDelete("/books/{id:int}", async (int id, LibraryDb db) =>
{
    var b = await db.Books.FindAsync(id);
    if (b is null) return Results.NotFound();
    db.Books.Remove(b);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

api.MapPost("/books/{id:int}/reviews", async (int id, ReviewInput i, LibraryDb db) =>
{
    if (i.Rating is < 1 or > 5) return Results.BadRequest(new { error = "Rating must be 1-5" });
    if (!await db.Books.AnyAsync(b => b.Id == id)) return Results.NotFound();
    var r = new Review { BookId = id, Rating = i.Rating, Text = i.Text };
    db.Reviews.Add(r);
    await db.SaveChangesAsync();
    return Results.Created($"/api/books/{id}", new ReviewDto(r.Id, r.Rating, r.Text, r.CreatedAt));
});

api.MapDelete("/reviews/{id:int}", async (int id, LibraryDb db) =>
{
    var r = await db.Reviews.FindAsync(id);
    if (r is null) return Results.NotFound();
    db.Reviews.Remove(r);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.Run();
