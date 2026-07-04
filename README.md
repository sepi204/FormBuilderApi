# Form Builder API

Backend starter for a Form Builder product built with **ASP.NET Core 8**, **EF Core**, **SQL Server**, and **JWT authentication**.

This phase implements only the authentication foundation (register, login, and a protected profile endpoint).

## Solution Structure

```
FormBuilder.sln
src/
  FormBuilder.Api/              # Web API, controllers, DTOs, JWT/Swagger setup
  FormBuilder.Domain/           # Domain entities
  FormBuilder.Infrastructure/   # EF Core, auth services, JWT token generation
```

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server or LocalDB (default connection string uses LocalDB)

## Configuration

Update `src/FormBuilder.Api/appsettings.json` if needed:

- `ConnectionStrings:DefaultConnection` — SQL Server connection string
- `Jwt:Issuer`, `Jwt:Audience`, `Jwt:SecretKey`, `Jwt:ExpirationMinutes` — JWT settings

## Commands

### Restore packages

```powershell
dotnet restore
```

### Build

```powershell
dotnet build
```

### Apply database migration

```powershell
dotnet ef database update --project src/FormBuilder.Infrastructure --startup-project src/FormBuilder.Api
```

### Run the API

```powershell
dotnet run --project src/FormBuilder.Api
```

Swagger UI is available in Development at:

- `https://localhost:<port>/swagger`

## API Endpoints

| Method | Route | Access | Description |
|--------|-------|--------|-------------|
| POST | `/api/auth/register` | Anonymous | Register a new user |
| POST | `/api/auth/login` | Anonymous | Login and receive JWT |
| GET | `/api/auth/me` | **Protected** | Get current user info |

All other endpoints require authentication by default (fallback authorization policy).

## Testing with Swagger

1. Call `POST /api/auth/register` with `username` and `password`.
2. Call `POST /api/auth/login` to get a JWT token.
3. Click **Authorize** in Swagger and enter: `Bearer {your-token}`
4. Call `GET /api/auth/me` to verify authentication.

## Notes

- Passwords are hashed with `PasswordHasher<User>` (never stored in plain text).
- User-facing messages and validation errors are in Persian.
- Code identifiers (namespaces, classes, routes) remain in English.
