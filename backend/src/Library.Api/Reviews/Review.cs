namespace Library.Api.Reviews;

public class Review
{
    public int Id { get; set; }
    public int BookId { get; set; }
    public int Rating { get; set; } // 1-5
    public string? Text { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public record ReviewInput(int Rating, string? Text)
{
    public const int MaxTextLength = 4000;

    public string? Validate() =>
        Rating is < 1 or > 5 ? "Rating must be 1-5" :
        Text is { Length: > MaxTextLength } ? $"Review must be at most {MaxTextLength} characters" : null;
}

public record ReviewDto(int Id, int Rating, string? Text, DateTime CreatedAt)
{
    public static ReviewDto From(Review r) => new(r.Id, r.Rating, r.Text, r.CreatedAt);
}
