# Auth Implementation — Task Status

**Last updated:** 2026-04-19  
**Scope:** `AuthController`, auth services, token management, external OAuth providers

---

## Overall Status

Authentication is fully implemented end-to-end for both Google OAuth and email/password. Result pattern is used consistently across all auth flows. `UserResponse` now carries `FirstName` and `LastName` via JWT claims. Microsoft OAuth remains an unimplemented stub.

---

## What's Done

### Google OAuth ✅

| Layer | File | Notes |
|-------|------|-------|
| Controller | `AuthController` — `POST /api/auth/external-login` | `[AllowAnonymous]`, Result pattern |
| Provider resolution | `AuthServiceFactory` | Case-insensitive LINQ over `IEnumerable<IAuthService>` |
| Token validation | `AuthGoogleService` | `Google.Apis.Auth` — cryptographic check, `EmailVerified` guard |
| User upsert | `UserService.CreateAuthUser()` | Find-or-create with `UserLogin` linkage |
| JWT issuance | `TokenService.CreateToken()` | HMAC-SHA256, 2h expiry, HttpOnly/Secure/SameSite=Strict cookie |
| DI | `AppExtensions.cs` | `AuthGoogleService` registered as `IAuthService` |
| Config | `appsettings.Development.json` | ClientId + Secret present (see security issues) |
| Tests | `AuthFactoryTests.cs` | Factory resolution and unknown-provider exception covered |

**Flow:** `Client sends Google IdToken → POST /external-login → factory resolves "google" → AuthGoogleService validates → UserService upserts User+UserLogin → TokenService issues JWT cookie → UserResponse (userId, email, firstName, lastName) returned`

---

### Email / Password Auth ✅

| Endpoint | Behaviour |
|----------|-----------|
| `POST /api/auth/login` | `[AllowAnonymous]` — validates credentials, issues JWT, returns `UserResponse` |
| `POST /api/auth/register` | `[AllowAnonymous]` — creates user, auto-login (issues JWT), returns `201 UserResponse` |
| `POST /api/auth/logout` | Deletes JWT cookie |
| `GET /api/auth/me` | Returns current user from JWT claims — no DB hit |

---

### Result Pattern ✅

All auth service methods return `Result<UserResponse>`. Controllers call `MapFailure(result)` — no manual status code handling.

| Failure case | Result status | Error code |
|---|---|---|
| Email already registered | `Conflict` | `EMAIL_TAKEN` |
| Invalid credentials | `Unauthorized` | `INVALID_CREDENTIALS` |
| Google token invalid/expired | `Unauthorized` | `GOOGLE_TOKEN_INVALID` |
| Google email not verified | `Unauthorized` | `GOOGLE_AUTH_FAILED` |
| Google network error | `Unauthorized` | `GOOGLE_AUTH_ERROR` |

---

### UserResponse ✅

All auth endpoints return a consistent `UserResponse`:

```json
{
  "userId": "guid",
  "email": "user@example.com",
  "firstName": "Maya",
  "lastName": "Smith"
}
```

`firstName` / `lastName` are nullable — email-registered users without a name return `null` until profile is updated. Names are stored as JWT claims (`given_name`, `family_name`) so `GET /me` resolves without a DB hit.

---

### RegisterRequest ✅

Dedicated `RegisterRequest` DTO with FluentValidation:

| Field | Validation |
|-------|-----------|
| `Email` | Required, valid email format |
| `Password` | Required, min 6 characters |
| `FirstName` | Optional, max 100 chars |
| `LastName` | Optional, max 100 chars |

---

### Infrastructure ✅

- `UserRepository` — `GetByEmail`, `GetEmailUser`
- `User.FirstName` / `User.LastName` — nullable (`string?`), migration `MakeUserNameNullable` applied
- EF config: `users` table, unique email index; `user_logins` table, composite unique index on `(Provider, ProviderKey)`
- JWT middleware: cookie extraction in `OnMessageReceived`, full token validation (issuer, audience, lifetime, zero clock skew)
- `AppControllerBase` — `[Authorize]` by default, `MapFailure<T>()` maps `Result<T>` to HTTP status codes
- `JwtSettings` — properties use `init` setters so config binder can populate them correctly

---

## What's Not Done

### 1. Refresh Token — Not Implemented
- `TokenService` has a `//TODO` comment
- Cookie expiry is 5 min, token expiry is 2 h — causes premature logout
- **Action:** Align cookie expiry with token lifetime as a quick fix; implement refresh token endpoint properly

### 2. Microsoft OAuth — Empty Stub
- `AuthMicroSoftService.cs` exists but has no implementation, not registered in DI
- **Action:** Implement using MSAL (`Microsoft.Identity.Client`), mirror `AuthGoogleService` pattern

### 3. AuthEmailService — Empty File
- `AuthEmailService.cs` is empty — email auth flows through `UserService` directly
- **Action:** Delete if not needed, or implement if a unified `IAuthService` abstraction for email is planned

### 4. `AuthSettings` Typo
- `GoogleSerect` should be `GoogleSecret`
- **Action:** Rename property and update all references

---

## Security Issues (Must Fix Before Production)

| Issue | Location | Severity |
|-------|----------|----------|
| Google OAuth secret in source | `appsettings.Development.json` | High — move to `dotnet user-secrets` or Azure Key Vault |
| JWT symmetric key in source | `appsettings.Development.json` | High — same as above |
| Cookie/token expiry mismatch | `TokenService` | Medium — 5 min cookie vs 2 h token causes early logout |
| No rate limiting on `/login` | `AuthController` | Medium — brute-force risk |
| No account lockout on failed logins | `UserService.Login()` | Medium |
| No email verification for email registration | `UserService.RegisterEmailUser()` | Medium |

---

## Suggested Next Steps (Priority Order)

1. **Fix cookie expiry** — align `CookieOptions.Expires` with 2h token lifetime
2. **Move secrets** — `dotnet user-secrets` for dev, Azure Key Vault for prod
3. **Rename `GoogleSerect` → `GoogleSecret`**
4. **Implement refresh token**
5. **Decide on `AuthEmailService`** — delete or implement
6. **Implement `AuthMicroSoftService`** — when Microsoft OAuth is required
7. **Add rate limiting** — `app.UseRateLimiter()` on `/login` and `/register`

---

## Architecture Notes

- Factory pattern is open/closed — adding a new OAuth provider only requires a new `IAuthService` implementation and a DI registration line; no factory changes needed.
- `IUserContext` decouples identity resolution from HTTP — services never touch `HttpContext` directly. Background jobs that need a user ID must pass it explicitly.
- `UserLogin` supports multiple providers per user. `CreateAuthUser` currently finds users by email and re-uses them for account linking — this is intentional.
- Name claims (`given_name`, `family_name`) in the JWT mean `GET /me` never hits the database. If names change after login, the JWT will reflect the old value until re-login.
