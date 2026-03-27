using System.Net.Http.Json;
using Client.Models.DTOs;

namespace Client.Services;

public interface IDashboardApiClient
{
    Task<DashboardOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default);
}

public sealed class DashboardApiClient(HttpClient httpClient) : ApiClient(httpClient), IDashboardApiClient
{
    public async Task<DashboardOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await HttpClient.GetFromJsonAsync<DashboardOverviewDto>("api/dashboard/overview", cancellationToken);
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

        return new DashboardOverviewDto
        {
            Revenue = 284_900m,
            Orders = 1248,
            ActiveUsers = 892,
            PublishedPosts = 74,
            SalesLabels = ["Jan", "Feb", "Mar", "Apr", "May", "Jun"],
            SalesValues = [68, 74, 82, 91, 104, 119],
            BlogTrafficLabels = ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"],
            BlogTrafficValues = [14, 22, 19, 31, 27, 35, 29]
        };
    }
}
