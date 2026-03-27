using System.Net.Http.Json;

namespace Client.Services;

public abstract class ApiClient(HttpClient httpClient)
{
    protected HttpClient HttpClient { get; } = httpClient;

    protected static async Task<T?> TryReadAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken = default)
    {
        if (!response.IsSuccessStatusCode)
        {
            return default;
        }

        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);
    }
}
