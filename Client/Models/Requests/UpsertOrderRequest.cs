using System.ComponentModel.DataAnnotations;

namespace Client.Models.Requests;

public sealed class UpsertOrderRequest
{
    public Guid? Id { get; set; }

    [Required]
    public string Number { get; set; } = string.Empty;

    [Required]
    public string Customer { get; set; } = string.Empty;

    [Range(0.01, 9999999)]
    public decimal Total { get; set; }

    [Required]
    public string FulfillmentStatus { get; set; } = string.Empty;

    [Required]
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
