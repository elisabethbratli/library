using Library.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Library.Api.Reviews;

public static class ReviewEndpoints
{
    public static void MapReviewEndpoints(this RouteGroupBuilder api)
    {
        api.MapPost("/books/{id:int}/reviews", async (int id, ReviewInput input, LibraryDb db) =>
        {
            if (input.Validate() is { } err) return Results.BadRequest(new { error = err });
            if (!await db.Books.AnyAsync(b => b.Id == id)) return Results.NotFound();
            var r = new Review
            {
                BookId = id, Rating = input.Rating,
                Text = string.IsNullOrWhiteSpace(input.Text) ? null : input.Text.Trim()
            };
            db.Reviews.Add(r);
            await db.SaveChangesAsync();
            return Results.Created($"/api/books/{id}", ReviewDto.From(r));
        });

        api.MapDelete("/reviews/{id:int}", async (int id, LibraryDb db) =>
        {
            var deleted = await db.Reviews.Where(r => r.Id == id).ExecuteDeleteAsync();
            return deleted == 0 ? Results.NotFound() : Results.NoContent();
        });
    }
}
