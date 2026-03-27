using System.Net.Http.Json;
using Client.Models.DTOs;
using Client.Models.Requests;
using MudBlazor;

namespace Client.Services;

public interface IWidgetCatalogApiClient
{
    Task<IReadOnlyList<DashboardWidgetDto>> GetWidgetsAsync(CancellationToken cancellationToken = default);
    Task<DashboardWidgetDto?> GetWidgetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DashboardWidgetDto> CreateWidgetAsync(UpsertDashboardWidgetRequest request, CancellationToken cancellationToken = default);
    Task<DashboardWidgetDto> UpdateWidgetAsync(Guid id, UpsertDashboardWidgetRequest request, CancellationToken cancellationToken = default);
    Task DeleteWidgetAsync(Guid id, CancellationToken cancellationToken = default);
}

public sealed class DashboardWidgetsApiClient : ApiClient, IWidgetCatalogApiClient
{
    private static readonly object SyncRoot = new();
    private static readonly List<DashboardWidgetDto> Widgets =
    [
        new() { Id = Guid.NewGuid(), Title = "Revenue", Value = "$284,900", Subtitle = "Gross revenue this month", Trend = "+12.8% MoM", Icon = Icons.Material.Outlined.AttachMoney, Color = nameof(Color.Success) },
        new() { Id = Guid.NewGuid(), Title = "Orders", Value = "1,248", Subtitle = "Orders processed", Trend = "+7.2% conversion", Icon = Icons.Material.Outlined.ReceiptLong, Color = nameof(Color.Primary) },
        new() { Id = Guid.NewGuid(), Title = "Active users", Value = "892", Subtitle = "7-day active users", Trend = "91% retention", Icon = Icons.Material.Outlined.PeopleAlt, Color = nameof(Color.Info) },
        new() { Id = Guid.NewGuid(), Title = "Published posts", Value = "74", Subtitle = "Live editorial assets", Trend = "3 scheduled today", Icon = Icons.Material.Outlined.Article, Color = nameof(Color.Warning) }
    ];

    public DashboardWidgetsApiClient(HttpClient httpClient) : base(httpClient)
    {
    }

    public async Task<IReadOnlyList<DashboardWidgetDto>> GetWidgetsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await HttpClient.GetFromJsonAsync<List<DashboardWidgetDto>>("api/dashboard/widgets", cancellationToken);
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

        await Task.Delay(100, cancellationToken);
        lock (SyncRoot)
        {
            return Widgets.Select(Clone).ToArray();
        }
    }

    public async Task<DashboardWidgetDto?> GetWidgetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await HttpClient.GetFromJsonAsync<DashboardWidgetDto>($"api/dashboard/widgets/{id}", cancellationToken);
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
            return Widgets.Where(widget => widget.Id == id).Select(Clone).FirstOrDefault();
        }
    }

    public async Task<DashboardWidgetDto> CreateWidgetAsync(UpsertDashboardWidgetRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await HttpClient.PostAsJsonAsync("api/dashboard/widgets", request, cancellationToken);
            var result = await TryReadAsync<DashboardWidgetDto>(response, cancellationToken);
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

        await Task.Delay(100, cancellationToken);
        var widget = Map(request, request.Id ?? Guid.NewGuid());
        lock (SyncRoot)
        {
            Widgets.Insert(0, widget);
        }

        return Clone(widget);
    }

    public async Task<DashboardWidgetDto> UpdateWidgetAsync(Guid id, UpsertDashboardWidgetRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await HttpClient.PutAsJsonAsync($"api/dashboard/widgets/{id}", request, cancellationToken);
            var result = await TryReadAsync<DashboardWidgetDto>(response, cancellationToken);
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

        await Task.Delay(100, cancellationToken);
        var updated = Map(request, id);
        lock (SyncRoot)
        {
            var index = Widgets.FindIndex(widget => widget.Id == id);
            if (index >= 0)
            {
                Widgets[index] = updated;
            }
        }

        return Clone(updated);
    }

    public async Task DeleteWidgetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await HttpClient.DeleteAsync($"api/dashboard/widgets/{id}", cancellationToken);
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

        await Task.Delay(100, cancellationToken);
        lock (SyncRoot)
        {
            Widgets.RemoveAll(widget => widget.Id == id);
        }
    }

    private static DashboardWidgetDto Map(UpsertDashboardWidgetRequest request, Guid id)
    {
        return new DashboardWidgetDto
        {
            Id = id,
            Title = request.Title,
            Value = request.Value,
            Subtitle = request.Subtitle,
            Trend = request.Trend,
            Icon = request.Icon,
            Color = request.Color
        };
    }

    private static DashboardWidgetDto Clone(DashboardWidgetDto widget)
    {
        return new DashboardWidgetDto
        {
            Id = widget.Id,
            Title = widget.Title,
            Value = widget.Value,
            Subtitle = widget.Subtitle,
            Trend = widget.Trend,
            Icon = widget.Icon,
            Color = widget.Color
        };
    }
}
