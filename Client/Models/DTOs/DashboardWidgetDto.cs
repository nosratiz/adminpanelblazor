namespace Client.Models.DTOs;

public sealed class DashboardWidgetDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Trend { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
}
