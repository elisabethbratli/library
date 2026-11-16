using Library.Api.Books;
using Library.Api.Reviews;
using Microsoft.EntityFrameworkCore;

namespace Library.Api.Data;

public class LibraryDb(DbContextOptions<LibraryDb> options) : DbContext(options)
{
    public DbSet<Book> Books => Set<Book>();
    public DbSet<Review> Reviews => Set<Review>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Book>(e =>
        {
            e.Property(x => x.Title).HasMaxLength(BookInput.MaxTextLength).IsRequired();
            e.Property(x => x.Author).HasMaxLength(BookInput.MaxTextLength).IsRequired();
            e.Property(x => x.Isbn).HasMaxLength(20);
            e.Property(x => x.CoverUrl).HasMaxLength(2000);
            e.HasMany(x => x.Reviews).WithOne().HasForeignKey(r => r.BookId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<Review>().Property(x => x.Text).HasMaxLength(ReviewInput.MaxTextLength);
    }
}
