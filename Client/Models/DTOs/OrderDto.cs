namespace Client.Models.DTOs;

public sealed class OrderDto
{
    public Guid Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public string Customer { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public string FulfillmentStatus { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }
}
