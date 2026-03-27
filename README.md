# SampleBlazor

SampleBlazor is a Blazor WebAssembly admin dashboard sample built with .NET 10 and MudBlazor. It demonstrates a clean operations UI for managing users, products, orders, blog content, reports, and dashboard widgets through typed API clients.

The project currently contains the client application. It is designed to call a backing HTTP API, but it also includes in-memory fallback behavior for most feature areas so the UI can still be explored when the API is unavailable.

## Overview

This sample is structured like a small internal operations portal:

- Dashboard with KPI widgets and charts
- User management with create, edit, delete, and password reset flows
- Product catalog management
- Order monitoring and editing
- Blog post management for editorial workflows
- Report definition management
- Dashboard widget management
- Login flow with JWT-based client authentication state

## Tech Stack

- .NET 10 Blazor WebAssembly
- MudBlazor for layout, forms, tables, charts, dialogs, and notifications
- Blazored.LocalStorage for client-side token persistence
- `HttpClientFactory` with typed API clients
- `AuthorizationCore` with a custom `AuthenticationStateProvider`

## How It Works

The client registers one public HTTP client for login requests and multiple authorized typed clients for feature modules. Authorized requests flow through a custom delegating handler that attaches a bearer token from local storage.

If the configured API is unreachable, several feature clients fall back to seeded in-memory data so the application can still be used as a demo. The authentication flow also supports a built-in demo login.

## Features

### Authentication

- Login page with a demo sign-in path
- JWT parsing on the client to build the current user identity
- Token storage in browser local storage
- Automatic redirect to the login page for protected routes

### Admin Modules

- `Users`: searchable grid, editor screen, delete flow, reset password action
- `Products`: searchable catalog grid and editor screen
- `Orders`: searchable order feed and editor screen
- `Blog`: editorial queue, metrics, and post editor
- `Reports`: report catalog, status metrics, and editor
- `Dashboard`: overview charts plus editable widgets

## Project Structure

```text
Client/
  Features/
    Auth/
    Blog/
    Dashboard/
    Orders/
    Products/
    Reports/
    Users/
  Infrastructure/
    Auth/
  Models/
    DTOs/
    Requests/
    Responses/
  Services/
  Shared/
  wwwroot/
```

## Getting Started

### Prerequisites

- .NET 10 SDK

### Restore

```bash
dotnet restore Client/Client.csproj
```

### Build

```bash
dotnet build Client/Client.csproj
```

### Run

```bash
dotnet run --project Client/Client.csproj
```

## Configuration

The client reads the API base URL from `Client/wwwroot/appsettings.json`:

```json
{
  "ApiBaseUrl": "https://localhost:5001/"
}
```

Update that value to point to your backend API.

## Demo Mode

If the API is not running, you can still explore the app with the built-in demo credentials:

- Email: `admin@sampleblazor.dev`
- Password: `Admin123!`

The demo mode is useful for UI review, component experimentation, and local development before a real backend is connected.

## API Expectations

The client is organized around typed feature clients and expects endpoints such as:

- `api/auth/login`
- `api/users`
- `api/products`
- `api/orders`
- `api/blog`
- `api/reports`
- `api/dashboard/overview`
- `api/dashboard/widgets`

Feature grids use a shared query model for pagination, sorting, and filtering.

## Why This Repo Exists

This repository is a useful starting point for:

- Blazor WebAssembly admin panel prototypes
- Frontend-first development against an incomplete backend
- MudBlazor CRUD patterns
- JWT-authenticated client-side dashboard applications

## Notes

- The repository currently focuses on the client application.
- Several services intentionally degrade to seeded local data when HTTP calls fail.
- The UI is optimized for demonstrating feature slices and application structure rather than production-hardening.