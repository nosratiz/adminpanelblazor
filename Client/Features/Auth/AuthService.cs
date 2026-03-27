using System.Net.Http;
using System.Net.Http.Json;
using Client.Infrastructure.Auth;
using Client.Models.Requests;
using Client.Models.Responses;

namespace Client.Features.Auth;

public interface IAuthService
{
    Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task LogoutAsync();
}

public sealed record AuthResult(bool Succeeded, string? ErrorMessage = null)
{
    public static AuthResult Success() => new(true);
    public static AuthResult Failure(string message) => new(false, message);
}

public sealed class AuthService(IHttpClientFactory httpClientFactory, CustomAuthenticationStateProvider authenticationStateProvider) : IAuthService
{
    public async Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = httpClientFactory.CreateClient("PublicApi");
            using var response = await client.PostAsJsonAsync("api/auth/login", request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken: cancellationToken);
                if (!string.IsNullOrWhiteSpace(authResponse?.AccessToken))
                {
                    await authenticationStateProvider.MarkUserAsAuthenticatedAsync(authResponse.AccessToken);
                    return AuthResult.Success();
                }
            }
        }
        catch (HttpRequestException)
        {
        }
        catch (TaskCanceledException)
        {
        }

        if (request.Email.Equals("admin@sampleblazor.dev", StringComparison.OrdinalIgnoreCase) && request.Password == "Admin123!")
        {
            var token = BuildDemoToken(request.Email);
            await authenticationStateProvider.MarkUserAsAuthenticatedAsync(token);
            return AuthResult.Success();
        }

        return AuthResult.Failure("Invalid credentials. Use admin@sampleblazor.dev / Admin123! for the built-in demo mode.");
    }

    public Task LogoutAsync()
    {
        return authenticationStateProvider.MarkUserAsLoggedOutAsync();
    }

    private static string BuildDemoToken(string email)
    {
        var header = JwtClaimsParser.Encode(new { alg = "none", typ = "JWT" });
        var payload = JwtClaimsParser.Encode(new
        {
            sub = Guid.NewGuid().ToString("N"),
            name = "Avery Stone",
            email,
            role = new[] { "Administrator" },
            exp = DateTimeOffset.UtcNow.AddHours(8).ToUnixTimeSeconds()
        });

        return $"{header}.{payload}.";
    }
}
