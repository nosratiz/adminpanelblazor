using Blazored.LocalStorage;
using Client.Features.Auth;
using Client.Infrastructure.Auth;
using Client.Services;
using Client.Shared;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<Client.App>("#app");

var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? builder.HostEnvironment.BaseAddress;

builder.Services.AddMudServices(configuration =>
{
    configuration.SnackbarConfiguration.PositionClass = MudBlazor.Defaults.Classes.Position.BottomRight;
    configuration.SnackbarConfiguration.PreventDuplicates = true;
    configuration.SnackbarConfiguration.ShowCloseIcon = true;
    configuration.SnackbarConfiguration.VisibleStateDuration = 3500;
});

builder.Services.AddBlazoredLocalStorage();
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AppState>();
builder.Services.AddScoped<JwtAuthorizationMessageHandler>();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
builder.Services.AddScoped(sp => (CustomAuthenticationStateProvider)sp.GetRequiredService<AuthenticationStateProvider>());

builder.Services.AddHttpClient("PublicApi", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
});

AddAuthorizedApiClient<IUsersApiClient, UsersApiClient>();
AddAuthorizedApiClient<IProductsApiClient, ProductsApiClient>();
AddAuthorizedApiClient<IOrdersApiClient, OrdersApiClient>();
AddAuthorizedApiClient<IBlogPostsApiClient, BlogPostsApiClient>();
AddAuthorizedApiClient<IReportsApiClient, ReportsApiClient>();
AddAuthorizedApiClient<IDashboardApiClient, DashboardApiClient>();
AddAuthorizedApiClient<IWidgetCatalogApiClient, DashboardWidgetsApiClient>();

builder.Services.AddScoped<IAuthService, AuthService>();

void AddAuthorizedApiClient<TClient, TImplementation>()
    where TClient : class
    where TImplementation : class, TClient
{
    builder.Services.AddHttpClient<TClient, TImplementation>(client =>
    {
        client.BaseAddress = new Uri(apiBaseUrl);
    }).AddHttpMessageHandler<JwtAuthorizationMessageHandler>();
}

await builder.Build().RunAsync();
