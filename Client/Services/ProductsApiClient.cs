using System.Net.Http.Json;
using Client.Models.DTOs;
using Client.Models.Requests;
using Client.Models.Responses;
using MudBlazor;

namespace Client.Services;

public interface IProductsApiClient
{
    Task<PagedResult<ProductDto>> GetProductsAsync(GridQueryRequest request, CancellationToken cancellationToken = default);
    Task<ProductDto?> GetProductAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProductDto> CreateProductAsync(UpsertProductRequest request, CancellationToken cancellationToken = default);
    Task<ProductDto> UpdateProductAsync(Guid id, UpsertProductRequest request, CancellationToken cancellationToken = default);
    Task DeleteProductAsync(Guid id, CancellationToken cancellationToken = default);
}

public sealed class ProductsApiClient : ApiClient, IProductsApiClient
{
    private static readonly object SyncRoot = new();
    private static readonly List<ProductDto> Products =
    [
        new() { Id = Guid.NewGuid(), Sku = "PRD-1001", Name = "Aero Bottle", Price = 29m, Stock = 122, Status = "Active" },
        new() { Id = Guid.NewGuid(), Sku = "PRD-1002", Name = "Studio Headphones", Price = 189m, Stock = 42, Status = "Active" },
        new() { Id = Guid.NewGuid(), Sku = "PRD-1003", Name = "Travel Sleeve", Price = 64m, Stock = 8, Status = "Low stock" },
        new() { Id = Guid.NewGuid(), Sku = "PRD-1004", Name = "Desk Lamp", Price = 95m, Stock = 0, Status = "Backorder" }
    ];

    public ProductsApiClient(HttpClient httpClient) : base(httpClient)
    {
    }

    public async Task<PagedResult<ProductDto>> GetProductsAsync(GridQueryRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await HttpClient.PostAsJsonAsync("api/products/query", request, cancellationToken);
            var result = await TryReadAsync<PagedResult<ProductDto>>(response, cancellationToken);
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
        return QueryProducts(request);
    }

    public async Task<ProductDto?> GetProductAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await HttpClient.GetFromJsonAsync<ProductDto>($"api/products/{id}", cancellationToken);
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
            return Products.Where(product => product.Id == id).Select(Clone).FirstOrDefault();
        }
    }

    public async Task<ProductDto> CreateProductAsync(UpsertProductRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await HttpClient.PostAsJsonAsync("api/products", request, cancellationToken);
            var result = await TryReadAsync<ProductDto>(response, cancellationToken);
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
        var product = Map(request, request.Id ?? Guid.NewGuid());
        lock (SyncRoot)
        {
            Products.Insert(0, product);
        }

        return Clone(product);
    }

    public async Task<ProductDto> UpdateProductAsync(Guid id, UpsertProductRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await HttpClient.PutAsJsonAsync($"api/products/{id}", request, cancellationToken);
            var result = await TryReadAsync<ProductDto>(response, cancellationToken);
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
            var index = Products.FindIndex(product => product.Id == id);
            if (index >= 0)
            {
                Products[index] = updated;
            }
        }

        return Clone(updated);
    }

    public async Task DeleteProductAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await HttpClient.DeleteAsync($"api/products/{id}", cancellationToken);
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
            Products.RemoveAll(product => product.Id == id);
        }
    }

    private static PagedResult<ProductDto> QueryProducts(GridQueryRequest request)
    {
        IEnumerable<ProductDto> query;
        lock (SyncRoot)
        {
            query = Products.Select(Clone).ToList();
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            query = query.Where(product =>
                product.Sku.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                product.Name.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                product.Status.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase));
        }

        var sort = request.Sorts.FirstOrDefault();
        query = sort?.Field switch
        {
            nameof(ProductDto.Sku) => query.OrderByDirection(ToDirection(sort.Descending), product => product.Sku),
            nameof(ProductDto.Name) => query.OrderByDirection(ToDirection(sort.Descending), product => product.Name),
            nameof(ProductDto.Price) => query.OrderByDirection(ToDirection(sort.Descending), product => product.Price),
            nameof(ProductDto.Stock) => query.OrderByDirection(ToDirection(sort.Descending), product => product.Stock),
            nameof(ProductDto.Status) => query.OrderByDirection(ToDirection(sort.Descending), product => product.Status),
            _ => query.OrderBy(product => product.Name)
        };

        var materialized = query.ToList();
        return new PagedResult<ProductDto>
        {
            TotalCount = materialized.Count,
            Items = materialized.Skip((Math.Max(request.Page, 1) - 1) * request.PageSize).Take(request.PageSize).ToArray()
        };
    }

    private static ProductDto Map(UpsertProductRequest request, Guid id)
    {
        return new ProductDto
        {
            Id = id,
            Sku = request.Sku,
            Name = request.Name,
            Price = request.Price,
            Stock = request.Stock,
            Status = request.Status
        };
    }

    private static ProductDto Clone(ProductDto product)
    {
        return new ProductDto
        {
            Id = product.Id,
            Sku = product.Sku,
            Name = product.Name,
            Price = product.Price,
            Stock = product.Stock,
            Status = product.Status
        };
    }

    private static SortDirection ToDirection(bool descending) => descending ? SortDirection.Descending : SortDirection.Ascending;
}
