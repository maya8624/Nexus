# Auth Implementation — Task Status

**Last updated:** 2026-04-19 (register auto-login + 201 implemented)  
**Scope:** `AuthController`, auth services, token management, external OAuth providers

---

## Overall Status

The authentication foundation is solid and follows good patterns (factory, repository, DI, options). Google OAuth is **fully wired end-to-end**. Email/password auth is functionally complete. Microsoft OAuth is a registered but empty stub.

---

## What's Done

### Google OAuth ✅ (Priority — Fully Implemented)

| Layer | File | Notes |
|-------|------|-------|
| Controller endpoint | `AuthController.cs` — `POST /api/auth/external-login` | `[AllowAnonymous]`, routes to factory |
| Provider resolution | `AuthServiceFactory.cs` | Case-insensitive LINQ lookup over `IEnumerable<IAuthService>` |
| Token validation | `AuthGoogleService.cs` | Uses `Google.Apis.Auth` — cryptographic JWT signature check, `EmailVerified` guard |
| User upsert | `UserService.CreateAuthUser()` | Find-or-create with `UserLogin` linkage (Provider + ProviderKey composite unique index) |
| JWT issuance | `TokenService.CreateToken()` | HMAC-SHA256, 2h expiry, HttpOnly/Secure/SameSite=Strict cookie |
| DI | `AppExtensions.cs` | `AuthGoogleService` registered as `IAuthService` |
| Config | `appsettings.Development.json` | ClientId + Secret present (see security issues below) |
| Tests | `AuthFactoryTests.cs` | Factory resolution and unknown-provider exception covered |

**Flow:** `Client sends Google IdToken → POST /external-login → AuthServiceFactory resolves "google" → AuthGoogleService validates with Google.Apis.Auth → UserService upserts User+UserLogin → TokenService issues HttpOnly JWT → UserResponse returned`

---

### Email / Password Auth ✅

- `POST /api/auth/login` — validates credentials, issues JWT, returns `bool` (consider returning `UserResponse` for consistency)
- `POST /api/auth/register` — hashes password, creates `User`, issues JWT cookie (auto-login), returns `201 UserResponse`
- `POST /api/auth/logout` — deletes JWT cookie
- `GET /api/auth/me` — reads current user from `ITokenService.GetCurrentUser()` (resolves from JWT claims via `IUserContext`)

---

### Infrastructure ✅

- `UserRepository` — `GetByEmail`, `GetEmailUser`
- EF config: `users` table, unique email index; `user_logins` table, composite unique index on `(Provider, ProviderKey)`
- JWT middleware: cookie extraction in `OnMessageReceived`, full token validation parameters (issuer, audience, lifetime, zero clock skew)
- `AppControllerBase` — `[Authorize]` by default, `MapFailure<T>()` maps domain `Result<T>` to HTTP status codes

---

## What's Not Done

### 1. Refresh Token — Not Implemented
- `TokenService.cs:57` has a TODO comment
- Current cookie expiry (5 min) is shorter than token expiry (2 h) — this will cause premature logouts
- **Action:** Implement refresh token endpoint or align cookie/token expiry durations

### 2. Microsoft OAuth — Empty Stub
- `AuthMicroSoftService.cs` — class exists but has zero implementation
- Not registered in DI (intentional until implemented)
- **Action:** Implement using `Microsoft.Identity.Client` (MSAL), mirror the `AuthGoogleService` pattern

### 3. AuthEmailService — Empty File
- `AuthEmailService.cs` is an empty file — unclear intent
- Email auth flows through `UserService` directly, not `IAuthService`
- **Action:** Clarify whether this file is needed; delete if not, or implement if a unified `IAuthService` abstraction for email is planned

### 4. Login Response Type — Inconsistency
- `POST /login` returns `bool` while `POST /external-login` returns `UserResponse`
- **Action:** Return `UserResponse` from login for a consistent client contract

### 5. `AuthSettings` Typo
- `GoogleSerect` should be `GoogleSecret`
- **Action:** Rename property and update all references

---

## Security Issues (Must Fix Before Production)

| Issue | Location | Severity |
|-------|----------|----------|
| Google OAuth secret in source | `appsettings.Development.json` | High — move to `dotnet user-secrets` or Azure Key Vault |
| JWT symmetric key in source | `appsettings.Development.json` | High — same as above |
| No rate limiting on `/login` | `AuthController` | Medium — brute-force risk |
| No account lockout on failed logins | `UserService.Login()` | Medium |
| No email verification for email registration | `UserService.RegisterEmailUser()` | Medium |
| Cookie/token expiry mismatch | `TokenService` | Medium — 5 min cookie vs 2 h token causes early logout |

---

## Suggested Next Steps (Priority Order)

1. **Fix cookie expiry** — align `CookieOptions.Expires` with token lifetime (quick, prevents logout bug)
2. **Move secrets** — use `dotnet user-secrets` for dev, Key Vault for prod
3. **Implement refresh token** — or extend cookie expiry as a short-term fix
4. **Fix `POST /login` return type** — return `UserResponse` instead of `bool`
5. **Rename `GoogleSerect` → `GoogleSecret`**
6. **Decide on `AuthEmailService`** — delete or implement
7. **Implement `AuthMicroSoftService`** — when Microsoft OAuth is required
8. **Add rate limiting** — `app.UseRateLimiter()` on `/login` and `/register`

---

## Architecture Notes

- Factory pattern (`IAuthServiceFactory` + `IEnumerable<IAuthService>`) is clean — adding a new provider only requires a new `IAuthService` implementation and DI registration; no factory code changes needed.
- `IUserContext` decouples identity resolution from HTTP concerns — services get `UserId` without touching `HttpContext` directly. Background jobs that need a user ID will need a different strategy (e.g., pass explicitly or use a scoped job context).
- `UserLogin` entity supports multiple providers per user (correct) but the `CreateAuthUser` upsert doesn't handle the case where a user tries to link a second provider with the same email — currently it finds the user by email and re-uses it, which is the expected behaviour for account linking.
