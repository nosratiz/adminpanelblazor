using System.ComponentModel.DataAnnotations;

namespace Client.Models.Requests;

public sealed class UpsertProductRequest
{
    public Guid? Id { get; set; }

    [Required]
    public string Sku { get; set; } = string.Empty;

    [Required]
    public string Name { get; set; } = string.Empty;

    [Range(0.01, 999999)]
    public decimal Price { get; set; }

    [Range(0, 100000)]
    public int Stock { get; set; }

    [Required]
    public string Status { get; set; } = string.Empty;
}
