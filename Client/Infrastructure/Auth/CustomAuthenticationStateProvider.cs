using Blazored.LocalStorage;
using Client.Shared;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace Client.Infrastructure.Auth;

public sealed class CustomAuthenticationStateProvider(ILocalStorageService localStorageService, AppState appState) : AuthenticationStateProvider
{
    private static readonly ClaimsPrincipal Anonymous = new(new ClaimsIdentity());

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await localStorageService.GetItemAsStringAsync(TokenStorageKeys.AccessToken);
        if (string.IsNullOrWhiteSpace(token))
        {
            appState.SetDisplayName(null);
            return new AuthenticationState(Anonymous);
        }

        var claims = JwtClaimsParser.ParseClaimsFromJwt(token);
        if (claims.Count == 0 || JwtClaimsParser.IsExpired(claims))
        {
            await localStorageService.RemoveItemAsync(TokenStorageKeys.AccessToken);
            appState.SetDisplayName(null);
            return new AuthenticationState(Anonymous);
        }

        var identity = new ClaimsIdentity(claims, authenticationType: "jwt");
        appState.SetDisplayName(JwtClaimsParser.GetDisplayName(claims));
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    public async Task<string?> GetTokenAsync()
    {
        var token = await localStorageService.GetItemAsStringAsync(TokenStorageKeys.AccessToken);
        return string.IsNullOrWhiteSpace(token) ? null : token;
    }

    public async Task MarkUserAsAuthenticatedAsync(string token)
    {
        await localStorageService.SetItemAsStringAsync(TokenStorageKeys.AccessToken, token);
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public async Task MarkUserAsLoggedOutAsync()
    {
        await localStorageService.RemoveItemAsync(TokenStorageKeys.AccessToken);
        appState.SetDisplayName(null);
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(Anonymous)));
    }
}
