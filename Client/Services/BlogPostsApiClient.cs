using System.Net.Http.Json;
using Client.Models.DTOs;
using Client.Models.Requests;
using Client.Models.Responses;
using MudBlazor;

namespace Client.Services;

public interface IBlogPostsApiClient
{
    Task<PagedResult<BlogPostDto>> GetBlogPostsAsync(GridQueryRequest request, CancellationToken cancellationToken = default);
    Task<BlogPostDto?> GetBlogPostAsync(Guid id, CancellationToken cancellationToken = default);
    Task<BlogPostDto> CreateBlogPostAsync(UpsertBlogPostRequest request, CancellationToken cancellationToken = default);
    Task<BlogPostDto> UpdateBlogPostAsync(Guid id, UpsertBlogPostRequest request, CancellationToken cancellationToken = default);
    Task DeleteBlogPostAsync(Guid id, CancellationToken cancellationToken = default);
}

public sealed class BlogPostsApiClient : ApiClient, IBlogPostsApiClient
{
    private static readonly object SyncRoot = new();
    private static readonly List<BlogPostDto> Posts =
    [
        new() { Id = Guid.NewGuid(), Title = "Platform release notes for April", Slug = "platform-release-notes-april", Category = "Release Notes", Status = "Scheduled", Author = "Avery Stone", PublishedUtc = DateTime.UtcNow.AddDays(1) },
        new() { Id = Guid.NewGuid(), Title = "Customer story: Helio Commerce", Slug = "customer-story-helio-commerce", Category = "Case Study", Status = "In Review", Author = "Mina Patel", PublishedUtc = null },
        new() { Id = Guid.NewGuid(), Title = "SEO refresh: Buying guide collection", Slug = "seo-refresh-buying-guide-collection", Category = "SEO", Status = "Draft", Author = "Lucas Grant", PublishedUtc = null },
        new() { Id = Guid.NewGuid(), Title = "Spring campaign preview", Slug = "spring-campaign-preview", Category = "Marketing", Status = "Published", Author = "Harper Reed", PublishedUtc = DateTime.UtcNow.AddDays(-3) }
    ];

    public BlogPostsApiClient(HttpClient httpClient) : base(httpClient)
    {
    }

    public async Task<PagedResult<BlogPostDto>> GetBlogPostsAsync(GridQueryRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await HttpClient.PostAsJsonAsync("api/blog/query", request, cancellationToken);
            var result = await TryReadAsync<PagedResult<BlogPostDto>>(response, cancellationToken);
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
        return QueryPosts(request);
    }

    public async Task<BlogPostDto?> GetBlogPostAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await HttpClient.GetFromJsonAsync<BlogPostDto>($"api/blog/{id}", cancellationToken);
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
            return Posts.Where(post => post.Id == id).Select(Clone).FirstOrDefault();
        }
    }

    public async Task<BlogPostDto> CreateBlogPostAsync(UpsertBlogPostRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await HttpClient.PostAsJsonAsync("api/blog", request, cancellationToken);
            var result = await TryReadAsync<BlogPostDto>(response, cancellationToken);
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
        var post = Map(request, request.Id ?? Guid.NewGuid());
        lock (SyncRoot)
        {
            Posts.Insert(0, post);
        }

        return Clone(post);
    }

    public async Task<BlogPostDto> UpdateBlogPostAsync(Guid id, UpsertBlogPostRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await HttpClient.PutAsJsonAsync($"api/blog/{id}", request, cancellationToken);
            var result = await TryReadAsync<BlogPostDto>(response, cancellationToken);
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
            var index = Posts.FindIndex(post => post.Id == id);
            if (index >= 0)
            {
                Posts[index] = updated;
            }
        }

        return Clone(updated);
    }

    public async Task DeleteBlogPostAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await HttpClient.DeleteAsync($"api/blog/{id}", cancellationToken);
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
            Posts.RemoveAll(post => post.Id == id);
        }
    }

    private static PagedResult<BlogPostDto> QueryPosts(GridQueryRequest request)
    {
        IEnumerable<BlogPostDto> query;
        lock (SyncRoot)
        {
            query = Posts.Select(Clone).ToList();
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            query = query.Where(post =>
                post.Title.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                post.Category.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                post.Status.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                post.Author.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase));
        }

        var sort = request.Sorts.FirstOrDefault();
        query = sort?.Field switch
        {
            nameof(BlogPostDto.Title) => query.OrderByDirection(ToDirection(sort.Descending), post => post.Title),
            nameof(BlogPostDto.Category) => query.OrderByDirection(ToDirection(sort.Descending), post => post.Category),
            nameof(BlogPostDto.Status) => query.OrderByDirection(ToDirection(sort.Descending), post => post.Status),
            nameof(BlogPostDto.Author) => query.OrderByDirection(ToDirection(sort.Descending), post => post.Author),
            nameof(BlogPostDto.PublishedUtc) => query.OrderByDirection(ToDirection(sort.Descending), post => post.PublishedUtc ?? DateTime.MinValue),
            _ => query.OrderByDescending(post => post.PublishedUtc ?? DateTime.MinValue)
        };

        var materialized = query.ToList();
        return new PagedResult<BlogPostDto>
        {
            TotalCount = materialized.Count,
            Items = materialized.Skip((Math.Max(request.Page, 1) - 1) * request.PageSize).Take(request.PageSize).ToArray()
        };
    }

    private static BlogPostDto Map(UpsertBlogPostRequest request, Guid id)
    {
        return new BlogPostDto
        {
            Id = id,
            Title = request.Title,
            Slug = request.Slug,
            Category = request.Category,
            Status = request.Status,
            Author = request.Author,
            PublishedUtc = request.PublishedUtc
        };
    }

    private static BlogPostDto Clone(BlogPostDto post)
    {
        return new BlogPostDto
        {
            Id = post.Id,
            Title = post.Title,
            Slug = post.Slug,
            Category = post.Category,
            Status = post.Status,
            Author = post.Author,
            PublishedUtc = post.PublishedUtc
        };
    }

    private static SortDirection ToDirection(bool descending) => descending ? SortDirection.Descending : SortDirection.Ascending;
}
