using System.Net.Http.Json;
using Client.Models.DTOs;
using Client.Models.Requests;
using Client.Models.Responses;
using MudBlazor;

namespace Client.Services;

public interface IOrdersApiClient
{
    Task<PagedResult<OrderDto>> GetOrdersAsync(GridQueryRequest request, CancellationToken cancellationToken = default);
    Task<OrderDto?> GetOrderAsync(Guid id, CancellationToken cancellationToken = default);
    Task<OrderDto> CreateOrderAsync(UpsertOrderRequest request, CancellationToken cancellationToken = default);
    Task<OrderDto> UpdateOrderAsync(Guid id, UpsertOrderRequest request, CancellationToken cancellationToken = default);
    Task DeleteOrderAsync(Guid id, CancellationToken cancellationToken = default);
}

public sealed class OrdersApiClient : ApiClient, IOrdersApiClient
{
    private static readonly object SyncRoot = new();
    private static readonly List<OrderDto> Orders =
    [
        new() { Id = Guid.NewGuid(), Number = "SO-24101", Customer = "Northwind Studio", Total = 1240m, FulfillmentStatus = "Packed", CreatedUtc = DateTime.UtcNow.AddHours(-4) },
        new() { Id = Guid.NewGuid(), Number = "SO-24102", Customer = "Everpeak Labs", Total = 344m, FulfillmentStatus = "Processing", CreatedUtc = DateTime.UtcNow.AddHours(-9) },
        new() { Id = Guid.NewGuid(), Number = "SO-24103", Customer = "Blue Valley", Total = 845m, FulfillmentStatus = "Delivered", CreatedUtc = DateTime.UtcNow.AddDays(-1) },
        new() { Id = Guid.NewGuid(), Number = "SO-24104", Customer = "Meridian House", Total = 216m, FulfillmentStatus = "Refund requested", CreatedUtc = DateTime.UtcNow.AddDays(-2) }
    ];

    public OrdersApiClient(HttpClient httpClient) : base(httpClient)
    {
    }

    public async Task<PagedResult<OrderDto>> GetOrdersAsync(GridQueryRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await HttpClient.PostAsJsonAsync("api/orders/query", request, cancellationToken);
            var result = await TryReadAsync<PagedResult<OrderDto>>(response, cancellationToken);
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
        return QueryOrders(request);
    }

    public async Task<OrderDto?> GetOrderAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await HttpClient.GetFromJsonAsync<OrderDto>($"api/orders/{id}", cancellationToken);
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
            return Orders.Where(order => order.Id == id).Select(Clone).FirstOrDefault();
        }
    }

    public async Task<OrderDto> CreateOrderAsync(UpsertOrderRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await HttpClient.PostAsJsonAsync("api/orders", request, cancellationToken);
            var result = await TryReadAsync<OrderDto>(response, cancellationToken);
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
        var order = Map(request, request.Id ?? Guid.NewGuid());
        lock (SyncRoot)
        {
            Orders.Insert(0, order);
        }

        return Clone(order);
    }

    public async Task<OrderDto> UpdateOrderAsync(Guid id, UpsertOrderRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await HttpClient.PutAsJsonAsync($"api/orders/{id}", request, cancellationToken);
            var result = await TryReadAsync<OrderDto>(response, cancellationToken);
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
            var index = Orders.FindIndex(order => order.Id == id);
            if (index >= 0)
            {
                Orders[index] = updated;
            }
        }

        return Clone(updated);
    }

    public async Task DeleteOrderAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await HttpClient.DeleteAsync($"api/orders/{id}", cancellationToken);
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
            Orders.RemoveAll(order => order.Id == id);
        }
    }

    private static PagedResult<OrderDto> QueryOrders(GridQueryRequest request)
    {
        IEnumerable<OrderDto> query;
        lock (SyncRoot)
        {
            query = Orders.Select(Clone).ToList();
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            query = query.Where(order =>
                order.Number.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                order.Customer.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                order.FulfillmentStatus.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase));
        }

        var sort = request.Sorts.FirstOrDefault();
        query = sort?.Field switch
        {
            nameof(OrderDto.Number) => query.OrderByDirection(ToDirection(sort.Descending), order => order.Number),
            nameof(OrderDto.Customer) => query.OrderByDirection(ToDirection(sort.Descending), order => order.Customer),
            nameof(OrderDto.Total) => query.OrderByDirection(ToDirection(sort.Descending), order => order.Total),
            nameof(OrderDto.FulfillmentStatus) => query.OrderByDirection(ToDirection(sort.Descending), order => order.FulfillmentStatus),
            nameof(OrderDto.CreatedUtc) => query.OrderByDirection(ToDirection(sort.Descending), order => order.CreatedUtc),
            _ => query.OrderByDescending(order => order.CreatedUtc)
        };

        var materialized = query.ToList();
        return new PagedResult<OrderDto>
        {
            TotalCount = materialized.Count,
            Items = materialized.Skip((Math.Max(request.Page, 1) - 1) * request.PageSize).Take(request.PageSize).ToArray()
        };
    }

    private static OrderDto Map(UpsertOrderRequest request, Guid id)
    {
        return new OrderDto
        {
            Id = id,
            Number = request.Number,
            Customer = request.Customer,
            Total = request.Total,
            FulfillmentStatus = request.FulfillmentStatus,
            CreatedUtc = request.CreatedUtc
        };
    }

    private static OrderDto Clone(OrderDto order)
    {
        return new OrderDto
        {
            Id = order.Id,
            Number = order.Number,
            Customer = order.Customer,
            Total = order.Total,
            FulfillmentStatus = order.FulfillmentStatus,
            CreatedUtc = order.CreatedUtc
        };
    }

    private static SortDirection ToDirection(bool descending) => descending ? SortDirection.Descending : SortDirection.Ascending;
}
