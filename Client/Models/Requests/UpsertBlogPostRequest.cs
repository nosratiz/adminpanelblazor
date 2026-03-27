using System.ComponentModel.DataAnnotations;

namespace Client.Models.Requests;

public sealed class UpsertBlogPostRequest
{
    public Guid? Id { get; set; }

    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Slug { get; set; } = string.Empty;

    [Required]
    public string Category { get; set; } = string.Empty;

    [Required]
    public string Status { get; set; } = string.Empty;

    [Required]
    public string Author { get; set; } = string.Empty;

    public DateTime? PublishedUtc { get; set; }
}
