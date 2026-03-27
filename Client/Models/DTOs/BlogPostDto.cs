namespace Client.Models.DTOs;

public sealed class BlogPostDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public DateTime? PublishedUtc { get; set; }
}
