using System.Net.Http.Json;
using Client.Models.DTOs;
using Client.Models.Requests;
using Client.Models.Responses;
using MudBlazor;

namespace Client.Services;

public interface IReportsApiClient
{
    Task<PagedResult<ReportDefinitionDto>> GetReportsAsync(GridQueryRequest request, CancellationToken cancellationToken = default);
    Task<ReportDefinitionDto?> GetReportAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ReportDefinitionDto> CreateReportAsync(UpsertReportDefinitionRequest request, CancellationToken cancellationToken = default);
    Task<ReportDefinitionDto> UpdateReportAsync(Guid id, UpsertReportDefinitionRequest request, CancellationToken cancellationToken = default);
    Task DeleteReportAsync(Guid id, CancellationToken cancellationToken = default);
}

public sealed class ReportsApiClient : ApiClient, IReportsApiClient
{
    private static readonly object SyncRoot = new();
    private static readonly List<ReportDefinitionDto> Reports =
    [
        new() { Id = Guid.NewGuid(), Name = "Executive weekly summary", Owner = "Avery Stone", Frequency = "Weekly", Status = "Healthy", LastRunUtc = DateTime.UtcNow.AddHours(-2) },
        new() { Id = Guid.NewGuid(), Name = "Revenue by region", Owner = "Elena Brooks", Frequency = "Daily", Status = "Healthy", LastRunUtc = DateTime.UtcNow.AddHours(-12) },
        new() { Id = Guid.NewGuid(), Name = "Inventory aging", Owner = "Noah Kim", Frequency = "Weekly", Status = "Needs review", LastRunUtc = DateTime.UtcNow.AddDays(-1) },
        new() { Id = Guid.NewGuid(), Name = "Content performance pack", Owner = "Mina Patel", Frequency = "Monthly", Status = "Paused", LastRunUtc = DateTime.UtcNow.AddDays(-5) }
    ];

    public ReportsApiClient(HttpClient httpClient) : base(httpClient)
    {
    }

    public async Task<PagedResult<ReportDefinitionDto>> GetReportsAsync(GridQueryRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await HttpClient.PostAsJsonAsync("api/reports/query", request, cancellationToken);
            var result = await TryReadAsync<PagedResult<ReportDefinitionDto>>(response, cancellationToken);
            if (result is not null)
            {
                return result;
            }
        }
        catch (HttpRequestException)
        {
        }
        catch (TaskCanceledException)
        {
        }

        await Task.Delay(120, cancellationToken);
        return QueryReports(request);
    }

    public async Task<ReportDefinitionDto?> GetReportAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await HttpClient.GetFromJsonAsync<ReportDefinitionDto>($"api/reports/{id}", cancellationToken);
            if (result is not null)
            {
                return result;
            }
        }
        catch (HttpRequestException)
        {
        }
        catch (TaskCanceledException)
        {
        }

        await Task.Delay(80, cancellationToken);
        lock (SyncRoot)
        {
            return Reports.Where(report => report.Id == id).Select(Clone).FirstOrDefault();
        }
    }

    public async Task<ReportDefinitionDto> CreateReportAsync(UpsertReportDefinitionRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await HttpClient.PostAsJsonAsync("api/reports", request, cancellationToken);
            var result = await TryReadAsync<ReportDefinitionDto>(response, cancellationToken);
            if (result is not null)
            {
                return result;
            }
        }
        catch (HttpRequestException)
        {
        }
        catch (TaskCanceledException)
        {
        }

        await Task.Delay(120, cancellationToken);
        var report = Map(request, request.Id ?? Guid.NewGuid());
        lock (SyncRoot)
        {
            Reports.Insert(0, report);
        }

        return Clone(report);
    }

    public async Task<ReportDefinitionDto> UpdateReportAsync(Guid id, UpsertReportDefinitionRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await HttpClient.PutAsJsonAsync($"api/reports/{id}", request, cancellationToken);
            var result = await TryReadAsync<ReportDefinitionDto>(response, cancellationToken);
            if (result is not null)
            {
                return result;
            }
        }
        catch (HttpRequestException)
        {
        }
        catch (TaskCanceledException)
        {
        }

        await Task.Delay(120, cancellationToken);
        var updated = Map(request, id);
        lock (SyncRoot)
        {
            var index = Reports.FindIndex(report => report.Id == id);
            if (index >= 0)
            {
                Reports[index] = updated;
            }
        }

        return Clone(updated);
    }

    public async Task DeleteReportAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await HttpClient.DeleteAsync($"api/reports/{id}", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return;
            }
        }
        catch (HttpRequestException)
        {
        }
        catch (TaskCanceledException)
        {
        }

        await Task.Delay(120, cancellationToken);
        lock (SyncRoot)
        {
            Reports.RemoveAll(report => report.Id == id);
        }
    }

    private static PagedResult<ReportDefinitionDto> QueryReports(GridQueryRequest request)
    {
        IEnumerable<ReportDefinitionDto> query;
        lock (SyncRoot)
        {
            query = Reports.Select(Clone).ToList();
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            query = query.Where(report =>
                report.Name.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                report.Owner.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                report.Frequency.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                report.Status.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase));
        }

        var sort = request.Sorts.FirstOrDefault();
        query = sort?.Field switch
        {
            nameof(ReportDefinitionDto.Name) => query.OrderByDirection(ToDirection(sort.Descending), report => report.Name),
            nameof(ReportDefinitionDto.Owner) => query.OrderByDirection(ToDirection(sort.Descending), report => report.Owner),
            nameof(ReportDefinitionDto.Frequency) => query.OrderByDirection(ToDirection(sort.Descending), report => report.Frequency),
            nameof(ReportDefinitionDto.Status) => query.OrderByDirection(ToDirection(sort.Descending), report => report.Status),
            nameof(ReportDefinitionDto.LastRunUtc) => query.OrderByDirection(ToDirection(sort.Descending), report => report.LastRunUtc),
            _ => query.OrderByDescending(report => report.LastRunUtc)
        };

        var materialized = query.ToList();
        return new PagedResult<ReportDefinitionDto>
        {
            TotalCount = materialized.Count,
            Items = materialized.Skip((Math.Max(request.Page, 1) - 1) * request.PageSize).Take(request.PageSize).ToArray()
        };
    }

    private static ReportDefinitionDto Map(UpsertReportDefinitionRequest request, Guid id)
    {
        return new ReportDefinitionDto
        {
            Id = id,
            Name = request.Name,
            Owner = request.Owner,
            Frequency = request.Frequency,
            Status = request.Status,
            LastRunUtc = request.LastRunUtc
        };
    }

    private static ReportDefinitionDto Clone(ReportDefinitionDto report)
    {
        return new ReportDefinitionDto
        {
            Id = report.Id,
            Name = report.Name,
            Owner = report.Owner,
            Frequency = report.Frequency,
            Status = report.Status,
            LastRunUtc = report.LastRunUtc
        };
    }

    private static SortDirection ToDirection(bool descending) => descending ? SortDirection.Descending : SortDirection.Ascending;
}
