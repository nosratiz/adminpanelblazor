using System.ComponentModel.DataAnnotations;

namespace Client.Models.Requests;

public sealed class UpsertDashboardWidgetRequest
{
    public Guid? Id { get; set; }

    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Value { get; set; } = string.Empty;

    public string Subtitle { get; set; } = string.Empty;
    public string Trend { get; set; } = string.Empty;

    [Required]
    public string Icon { get; set; } = string.Empty;

    [Required]
    public string Color { get; set; } = string.Empty;
}
