namespace Client.Models.DTOs;

public sealed class ReportDefinitionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public string Frequency { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime LastRunUtc { get; set; }
}
