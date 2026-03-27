namespace Client.Models.DTOs;

public sealed class DashboardOverviewDto
{
    public decimal Revenue { get; set; }
    public int Orders { get; set; }
    public int ActiveUsers { get; set; }
    public int PublishedPosts { get; set; }
    public string[] SalesLabels { get; set; } = [];
    public double[] SalesValues { get; set; } = [];
    public string[] BlogTrafficLabels { get; set; } = [];
    public double[] BlogTrafficValues { get; set; } = [];
}
