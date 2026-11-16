using Library.Api.Data;
using Library.Api.Reviews;
using Microsoft.EntityFrameworkCore;

namespace Library.Api.Books;

public static class BookEndpoints
{
    public static void MapBookEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/books", async (string? q, LibraryDb db) =>
        {
            var query = db.Books.Include(b => b.Reviews).AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = $"%{q.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_")}%";
                query = query.Where(b => EF.Functions.ILike(b.Title, term) || EF.Functions.ILike(b.Author, term));
            }
            return (await query.OrderBy(b => b.Title).ToListAsync()).Select(BookDto.From);
        });

        api.MapGet("/books/{id:int}", async (int id, LibraryDb db) =>
        {
            var b = await db.Books.Include(x => x.Reviews).FirstOrDefaultAsync(x => x.Id == id);
            return b is null
                ? Results.NotFound()
                : Results.Ok(new BookDetailDto(BookDto.From(b),
                    b.Reviews.OrderByDescending(r => r.CreatedAt).Select(ReviewDto.From).ToList()));
        });

        api.MapPost("/books", async (BookInput input, LibraryDb db) =>
        {
            if (input.Validate() is { } err) return Results.BadRequest(new { error = err });
            var b = new Book();
            input.ApplyTo(b);
            db.Books.Add(b);
            await db.SaveChangesAsync();
            return Results.Created($"/api/books/{b.Id}", BookDto.From(b));
        });

        api.MapPut("/books/{id:int}", async (int id, BookInput input, LibraryDb db) =>
        {
            if (input.Validate() is { } err) return Results.BadRequest(new { error = err });
            var b = await db.Books.Include(x => x.Reviews).FirstOrDefaultAsync(x => x.Id == id);
            if (b is null) return Results.NotFound();
            input.ApplyTo(b);
            await db.SaveChangesAsync();
            return Results.Ok(BookDto.From(b));
        });

        api.MapDelete("/books/{id:int}", async (int id, LibraryDb db) =>
        {
            var deleted = await db.Books.Where(b => b.Id == id).ExecuteDeleteAsync(); // cascades to reviews
            return deleted == 0 ? Results.NotFound() : Results.NoContent();
        });
    }
}
