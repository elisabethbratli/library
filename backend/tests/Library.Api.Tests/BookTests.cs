using System.Net;
using System.Text.Json;

namespace Library.Api.Tests;

public class BookTests : IClassFixture<LibraryFactory>
{
    private readonly LibraryFactory _f;
    public BookTests(LibraryFactory f) => _f = f;

    private static async Task<JsonElement> Json(HttpResponseMessage r) =>
        JsonDocument.Parse(await r.Content.ReadAsStringAsync()).RootElement;

    private static async Task<int> AddBook(HttpClient c, string title, string author = "Someone")
    {
        var res = await c.PostAsJsonAsync("/api/books", new { title, author });
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        return (await Json(res)).GetProperty("id").GetInt32();
    }

    [Fact]
    public async Task Create_then_get_book_with_no_reviews()
    {
        var c = await _f.LoggedInClientAsync();
        var res = await c.PostAsJsonAsync("/api/books", new
        {
            title = "  Dune ", author = "Frank Herbert", isbn = "978-0441172719", publishedYear = 1965
        });
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        Assert.NotNull(res.Headers.Location);

        var detail = await Json(await c.GetAsync(res.Headers.Location));
        var book = detail.GetProperty("book");
        Assert.Equal("Dune", book.GetProperty("title").GetString()); // trimmed
        Assert.Equal(JsonValueKind.Null, book.GetProperty("averageRating").ValueKind);
        Assert.Equal(0, book.GetProperty("reviewCount").GetInt32());
    }

    [Theory]
    [InlineData("", "Author", null, "Title is required")]
    [InlineData("   ", "Author", null, "Title is required")]
    [InlineData("Title", "", null, "Author is required")]
    [InlineData("Title", "Author", "javascript:alert(1)", "Cover URL must be an http(s) URL")]
    public async Task Invalid_book_is_rejected(string title, string author, string? coverUrl, string error)
    {
        var c = await _f.LoggedInClientAsync();
        var res = await c.PostAsJsonAsync("/api/books", new { title, author, coverUrl });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Equal(error, (await Json(res)).GetProperty("error").GetString());
    }

    [Fact]
    public async Task Update_book()
    {
        var c = await _f.LoggedInClientAsync();
        var id = await AddBook(c, "Old title");
        var res = await c.PutAsJsonAsync($"/api/books/{id}", new { title = "New title", author = "New author" });
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Equal("New title", (await Json(res)).GetProperty("title").GetString());
        Assert.Equal(HttpStatusCode.NotFound, (await c.PutAsJsonAsync("/api/books/999999", new { title = "t", author = "a" })).StatusCode);
    }

    [Fact]
    public async Task Search_matches_title_or_author_case_insensitively_and_escapes_wildcards()
    {
        var c = await _f.LoggedInClientAsync();
        await AddBook(c, "Zebra Handbook", "Quincy Quill");
        await AddBook(c, "Other", "Zed Zebra-Smith");
        await AddBook(c, "Unrelated", "Nobody");

        async Task<string[]> Search(string q) =>
            (await Json(await c.GetAsync($"/api/books?q={Uri.EscapeDataString(q)}")))
            .EnumerateArray().Select(b => b.GetProperty("title").GetString()!).ToArray();

        Assert.Equal(["Other", "Zebra Handbook"], await Search("zEbRa"));
        Assert.Equal(["Zebra Handbook"], await Search("quincy"));
        Assert.Empty(await Search("%")); // literal percent, not "match everything"
    }

    [Fact]
    public async Task Missing_book_returns_404()
    {
        var c = await _f.LoggedInClientAsync();
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync("/api/books/999999")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c.DeleteAsync("/api/books/999999")).StatusCode);
    }

    [Fact]
    public async Task Reviews_update_average_and_are_listed_newest_first()
    {
        var c = await _f.LoggedInClientAsync();
        var id = await AddBook(c, "Reviewed");
        Assert.Equal(HttpStatusCode.Created, (await c.PostAsJsonAsync($"/api/books/{id}/reviews", new { rating = 5, text = "first" })).StatusCode);
        await Task.Delay(20);
        Assert.Equal(HttpStatusCode.Created, (await c.PostAsJsonAsync($"/api/books/{id}/reviews", new { rating = 2, text = "second" })).StatusCode);

        var detail = await Json(await c.GetAsync($"/api/books/{id}"));
        Assert.Equal(3.5, detail.GetProperty("book").GetProperty("averageRating").GetDouble());
        Assert.Equal(2, detail.GetProperty("book").GetProperty("reviewCount").GetInt32());
        Assert.Equal(["second", "first"],
            detail.GetProperty("reviews").EnumerateArray().Select(r => r.GetProperty("text").GetString()!).ToArray());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    public async Task Review_rating_must_be_1_to_5(int rating)
    {
        var c = await _f.LoggedInClientAsync();
        var id = await AddBook(c, "Rated");
        var res = await c.PostAsJsonAsync($"/api/books/{id}/reviews", new { rating });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Review_for_missing_book_returns_404()
    {
        var c = await _f.LoggedInClientAsync();
        Assert.Equal(HttpStatusCode.NotFound, (await c.PostAsJsonAsync("/api/books/999999/reviews", new { rating = 3 })).StatusCode);
    }

    [Fact]
    public async Task Delete_review_and_cascade_delete_with_book()
    {
        var c = await _f.LoggedInClientAsync();
        var id = await AddBook(c, "To delete");
        var r1 = (await Json(await c.PostAsJsonAsync($"/api/books/{id}/reviews", new { rating = 4 }))).GetProperty("id").GetInt32();
        var r2 = (await Json(await c.PostAsJsonAsync($"/api/books/{id}/reviews", new { rating = 1 }))).GetProperty("id").GetInt32();

        Assert.Equal(HttpStatusCode.NoContent, (await c.DeleteAsync($"/api/reviews/{r1}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c.DeleteAsync($"/api/reviews/{r1}")).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await c.DeleteAsync($"/api/books/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync($"/api/books/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c.DeleteAsync($"/api/reviews/{r2}")).StatusCode); // cascaded
    }
}
