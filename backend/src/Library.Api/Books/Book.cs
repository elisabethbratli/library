using Library.Api.Reviews;

namespace Library.Api.Books;

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
