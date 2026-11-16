using Library.Api.Data;
using Library.Api.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Connection string: ConnectionStrings__Library env var (set by the container in Scaleway)
builder.Services.AddDbContext<LibraryDb>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("Library")
        ?? "Host=localhost;Database=library;Username=library;Password=library"));

var origins = builder.Configuration["AllowedOrigins"]?.Split(',', StringSplitOptions.RemoveEmptyEntries);
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
{
    if (origins is { Length: > 0 }) p.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
    else p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
}));

var app = builder.Build();
app.UseCors();

// Create schema on startup. For a small home app this is enough; switch to EF migrations if the schema grows.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LibraryDb>();
    await db.Database.EnsureCreatedAsync();
}

app.MapGet("/healthz", () => Results.Ok("ok"));

var api = app.MapGroup("/api");

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
