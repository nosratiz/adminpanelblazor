namespace Client.Models.Requests;

public sealed class GridQueryRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SearchTerm { get; set; }
    public List<SortDescriptor> Sorts { get; set; } = [];
    public List<FilterDescriptor> Filters { get; set; } = [];
}

public sealed class SortDescriptor
{
    public string Field { get; set; } = string.Empty;
    public bool Descending { get; set; }
}

public sealed class FilterDescriptor
{
    public string Field { get; set; } = string.Empty;
    public string Operator { get; set; } = string.Empty;
    public string? Value { get; set; }
}
