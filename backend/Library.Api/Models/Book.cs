namespace Library.Api.Models;

public class Book
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Author { get; set; } = "";
    public string? Isbn { get; set; }
    public int? PublishedYear { get; set; }
    public string? Description { get; set; }
    public string? CoverUrl { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    public List<Review> Reviews { get; set; } = [];
}

public class Review
{
    public int Id { get; set; }
    public int BookId { get; set; }
    public int Rating { get; set; } // 1-5
    public string? Text { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public record BookInput(string Title, string Author, string? Isbn, int? PublishedYear, string? Description, string? CoverUrl);
public record ReviewInput(int Rating, string? Text);
public record ReviewDto(int Id, int Rating, string? Text, DateTime CreatedAt);
public record BookDto(int Id, string Title, string Author, string? Isbn, int? PublishedYear,
    string? Description, string? CoverUrl, DateTime AddedAt, double? AverageRating, int ReviewCount);
public record BookDetailDto(BookDto Book, List<ReviewDto> Reviews);
public record LoginInput(string? Password);
