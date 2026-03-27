using System.ComponentModel.DataAnnotations;

namespace Client.Models.Requests;

public sealed class UpsertReportDefinitionRequest
{
    public Guid? Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Owner { get; set; } = string.Empty;

    [Required]
    public string Frequency { get; set; } = string.Empty;

    [Required]
    public string Status { get; set; } = string.Empty;

    public DateTime LastRunUtc { get; set; } = DateTime.UtcNow;
}
