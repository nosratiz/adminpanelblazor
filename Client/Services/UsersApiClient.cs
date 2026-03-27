using System.Net.Http.Json;
using Client.Models.DTOs;
using Client.Models.Requests;
using Client.Models.Responses;
using MudBlazor;

namespace Client.Services;

public interface IUsersApiClient
{
    Task<PagedResult<UserDto>> GetUsersAsync(GridQueryRequest request, CancellationToken cancellationToken = default);
    Task<UserDto?> GetUserAsync(Guid id, CancellationToken cancellationToken = default);
    Task<UserDto> CreateUserAsync(UpsertUserRequest request, CancellationToken cancellationToken = default);
    Task<UserDto> UpdateUserAsync(Guid id, UpsertUserRequest request, CancellationToken cancellationToken = default);
    Task DeleteUserAsync(Guid id, CancellationToken cancellationToken = default);
    Task ResetPasswordAsync(Guid id, CancellationToken cancellationToken = default);
}

public sealed class UsersApiClient : ApiClient, IUsersApiClient
{
    private static readonly object SyncRoot = new();
    private static readonly List<UserDto> Users = SeedUsers().ToList();

    public UsersApiClient(HttpClient httpClient) : base(httpClient)
    {
    }

    public async Task<PagedResult<UserDto>> GetUsersAsync(GridQueryRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await HttpClient.PostAsJsonAsync("api/users/query", request, cancellationToken);
            var result = await TryReadAsync<PagedResult<UserDto>>(response, cancellationToken);
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

        await Task.Delay(180, cancellationToken);
        return BuildFallbackResult(request);
    }

    public async Task<UserDto?> GetUserAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await HttpClient.GetFromJsonAsync<UserDto>($"api/users/{id}", cancellationToken);
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
            return Users.Where(user => user.Id == id).Select(Clone).FirstOrDefault();
        }
    }

    public async Task<UserDto> CreateUserAsync(UpsertUserRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await HttpClient.PostAsJsonAsync("api/users", request, cancellationToken);
            var result = await TryReadAsync<UserDto>(response, cancellationToken);
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
        var user = Map(request, request.Id ?? Guid.NewGuid(), DateTime.UtcNow);
        lock (SyncRoot)
        {
            Users.Insert(0, user);
        }

        return Clone(user);
    }

    public async Task<UserDto> UpdateUserAsync(Guid id, UpsertUserRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await HttpClient.PutAsJsonAsync($"api/users/{id}", request, cancellationToken);
            var result = await TryReadAsync<UserDto>(response, cancellationToken);
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
        UserDto updated;
        lock (SyncRoot)
        {
            var existing = Users.FirstOrDefault(user => user.Id == id);
            var lastActiveUtc = existing?.LastActiveUtc ?? DateTime.UtcNow;
            updated = Map(request, id, lastActiveUtc);
            var index = Users.FindIndex(user => user.Id == id);
            if (index >= 0)
            {
                Users[index] = updated;
            }
        }

        return Clone(updated);
    }

    public async Task DeleteUserAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await HttpClient.DeleteAsync($"api/users/{id}", cancellationToken);
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
            Users.RemoveAll(user => user.Id == id);
        }
    }

    public async Task ResetPasswordAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await HttpClient.PostAsync($"api/users/{id}/reset-password", content: null, cancellationToken);
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
    }

    private static PagedResult<UserDto> BuildFallbackResult(GridQueryRequest request)
    {
        IEnumerable<UserDto> query;
        lock (SyncRoot)
        {
            query = Users.Select(Clone).ToList();
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            query = query.Where(user =>
                user.FullName.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                user.Email.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                user.Role.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                user.Status.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var filter in request.Filters.Where(filter => !string.IsNullOrWhiteSpace(filter.Value)))
        {
            query = ApplyFilter(query, filter);
        }

        var sort = request.Sorts.FirstOrDefault();
        query = sort is null ? query.OrderByDescending(user => user.LastActiveUtc) : ApplySort(query, sort);

        var materializedQuery = query.ToList();
        var totalCount = materializedQuery.Count;
        var items = materializedQuery
            .Skip((Math.Max(request.Page, 1) - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToArray();

        return new PagedResult<UserDto>
        {
            Items = items,
            TotalCount = totalCount
        };
    }

    private static IEnumerable<UserDto> ApplyFilter(IEnumerable<UserDto> users, FilterDescriptor filter)
    {
        return filter.Field switch
        {
            nameof(UserDto.FullName) => users.Where(user => user.FullName.Contains(filter.Value!, StringComparison.OrdinalIgnoreCase)),
            nameof(UserDto.Email) => users.Where(user => user.Email.Contains(filter.Value!, StringComparison.OrdinalIgnoreCase)),
            nameof(UserDto.Role) => users.Where(user => user.Role.Contains(filter.Value!, StringComparison.OrdinalIgnoreCase)),
            nameof(UserDto.Status) => users.Where(user => user.Status.Contains(filter.Value!, StringComparison.OrdinalIgnoreCase)),
            _ => users
        };
    }

    private static IEnumerable<UserDto> ApplySort(IEnumerable<UserDto> users, SortDescriptor sort)
    {
        return sort.Field switch
        {
            nameof(UserDto.FullName) => users.OrderByDirection(ToDirection(sort.Descending), user => user.FullName),
            nameof(UserDto.Email) => users.OrderByDirection(ToDirection(sort.Descending), user => user.Email),
            nameof(UserDto.Role) => users.OrderByDirection(ToDirection(sort.Descending), user => user.Role),
            nameof(UserDto.Status) => users.OrderByDirection(ToDirection(sort.Descending), user => user.Status),
            _ => users.OrderByDirection(ToDirection(sort.Descending), user => user.LastActiveUtc)
        };
    }

    private static SortDirection ToDirection(bool descending) => descending ? SortDirection.Descending : SortDirection.Ascending;

    private static IReadOnlyList<UserDto> SeedUsers()
    {
        return new[]
        {
            new UserDto { Id = Guid.NewGuid(), FullName = "Avery Stone", Email = "avery@sampleblazor.dev", Role = "Administrator", Status = "Active", LastActiveUtc = DateTime.UtcNow.AddMinutes(-5) },
            new UserDto { Id = Guid.NewGuid(), FullName = "Mina Patel", Email = "mina@sampleblazor.dev", Role = "Editor", Status = "Active", LastActiveUtc = DateTime.UtcNow.AddMinutes(-18) },
            new UserDto { Id = Guid.NewGuid(), FullName = "Lucas Grant", Email = "lucas@sampleblazor.dev", Role = "Support", Status = "Pending", LastActiveUtc = DateTime.UtcNow.AddHours(-2) },
            new UserDto { Id = Guid.NewGuid(), FullName = "Harper Reed", Email = "harper@sampleblazor.dev", Role = "Manager", Status = "Active", LastActiveUtc = DateTime.UtcNow.AddHours(-4) },
            new UserDto { Id = Guid.NewGuid(), FullName = "Noah Kim", Email = "noah@sampleblazor.dev", Role = "Administrator", Status = "Suspended", LastActiveUtc = DateTime.UtcNow.AddDays(-1) },
            new UserDto { Id = Guid.NewGuid(), FullName = "Elena Brooks", Email = "elena@sampleblazor.dev", Role = "Analyst", Status = "Active", LastActiveUtc = DateTime.UtcNow.AddMinutes(-36) },
            new UserDto { Id = Guid.NewGuid(), FullName = "Tobias Cole", Email = "tobias@sampleblazor.dev", Role = "Support", Status = "Active", LastActiveUtc = DateTime.UtcNow.AddHours(-7) },
            new UserDto { Id = Guid.NewGuid(), FullName = "Ivy Chen", Email = "ivy@sampleblazor.dev", Role = "Editor", Status = "Pending", LastActiveUtc = DateTime.UtcNow.AddHours(-9) },
            new UserDto { Id = Guid.NewGuid(), FullName = "Sage Walker", Email = "sage@sampleblazor.dev", Role = "Manager", Status = "Active", LastActiveUtc = DateTime.UtcNow.AddDays(-2) },
            new UserDto { Id = Guid.NewGuid(), FullName = "Omar Rahman", Email = "omar@sampleblazor.dev", Role = "Analyst", Status = "Active", LastActiveUtc = DateTime.UtcNow.AddMinutes(-12) },
            new UserDto { Id = Guid.NewGuid(), FullName = "Riley Perez", Email = "riley@sampleblazor.dev", Role = "Support", Status = "Suspended", LastActiveUtc = DateTime.UtcNow.AddDays(-3) },
            new UserDto { Id = Guid.NewGuid(), FullName = "Clara Hall", Email = "clara@sampleblazor.dev", Role = "Editor", Status = "Active", LastActiveUtc = DateTime.UtcNow.AddHours(-13) }
        };
    }

    private static UserDto Map(UpsertUserRequest request, Guid id, DateTime lastActiveUtc)
    {
        return new UserDto
        {
            Id = id,
            FullName = request.FullName,
            Email = request.Email,
            Role = request.Role,
            Status = request.Status,
            LastActiveUtc = lastActiveUtc
        };
    }

    private static UserDto Clone(UserDto user)
    {
        return new UserDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            Status = user.Status,
            LastActiveUtc = user.LastActiveUtc
        };
    }
}
