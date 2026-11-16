namespace Library.Api.Books;

public record BookInput(string? Title, string? Author, string? Isbn, int? PublishedYear, string? Description, string? CoverUrl)
{
    public const int MaxTextLength = 300;

    /// <summary>Returns an error message, or null when the input is valid.</summary>
    public string? Validate()
    {
        if (string.IsNullOrWhiteSpace(Title)) return "Title is required";
        if (string.IsNullOrWhiteSpace(Author)) return "Author is required";
        if (Title.Trim().Length > MaxTextLength || Author.Trim().Length > MaxTextLength)
            return $"Title and author must be at most {MaxTextLength} characters";
        if (Isbn is { Length: > 20 }) return "ISBN is too long";
        if (PublishedYear is < 0 or > 3000) return "Published year is out of range";
        if (CoverUrl is { Length: > 0 } &&
            !(Uri.TryCreate(CoverUrl, UriKind.Absolute, out var u) && u.Scheme is "http" or "https"))
            return "Cover URL must be an http(s) URL";
        return null;
    }

    public void ApplyTo(Book b)
    {
        b.Title = Title!.Trim();
        b.Author = Author!.Trim();
        b.Isbn = string.IsNullOrWhiteSpace(Isbn) ? null : Isbn.Trim();
        b.PublishedYear = PublishedYear;
        b.Description = string.IsNullOrWhiteSpace(Description) ? null : Description;
        b.CoverUrl = string.IsNullOrWhiteSpace(CoverUrl) ? null : CoverUrl.Trim();
    }
}

public record BookDto(int Id, string Title, string Author, string? Isbn, int? PublishedYear,
    string? Description, string? CoverUrl, DateTime AddedAt, double? AverageRating, int ReviewCount)
{
    public static BookDto From(Book b) => new(b.Id, b.Title, b.Author, b.Isbn, b.PublishedYear, b.Description,
        b.CoverUrl, b.AddedAt, b.Reviews.Count == 0 ? null : b.Reviews.Average(r => r.Rating), b.Reviews.Count);
}

public record BookDetailDto(BookDto Book, List<Reviews.ReviewDto> Reviews);
