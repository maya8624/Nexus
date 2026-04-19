# Auth — Frontend Integration Reference

**API base URL (dev):** `http://localhost:5173` proxied to `https://localhost:7289`  
**All requests must include:** `credentials: "include"` (cookie-based auth)

---

## How Auth Works

The API uses **HttpOnly cookies** — the frontend never touches the JWT directly. After any successful login/register, the server sets a `__Host-Nexus-Auth` cookie automatically. Every subsequent request sends this cookie automatically as long as `credentials: "include"` is set.

```
Client                          API
  |                              |
  |-- POST /login (email+pass) ->|
  |<- 200 + Set-Cookie JWT -------|
  |                              |
  |-- GET /any-protected (cookie)|
  |<- 200 data ------------------|
```

> **Important:** Never store or read the JWT manually. The browser manages the cookie. Do not put the token in localStorage or Authorization headers.

---

## Endpoints

### POST `/api/auth/register`

Register a new user with email and password. Auto-logs in on success (sets cookie).

**Request**
```json
{
  "email": "user@example.com",
  "password": "secret123"
}
```

**Success — `201 Created`**
```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "email": "user@example.com"
}
```

**Failure — `409 Conflict`** (email already registered)
```json
{
  "code": 409,
  "name": "EMAIL_TAKEN",
  "message": "Email already registered"
}
```

---

### POST `/api/auth/login`

Login with email and password. Sets cookie on success.

**Request**
```json
{
  "email": "user@example.com",
  "password": "secret123"
}
```

**Success — `200 OK`**
```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "email": "user@example.com"
}
```

**Failure — `401 Unauthorized`**
```json
{
  "code": 401,
  "name": "INVALID_CREDENTIALS",
  "message": "Invalid email or password"
}
```

---

### POST `/api/auth/external-login`

Login or register via an external OAuth provider (currently: Google). Sets cookie on success.

**Request**
```json
{
  "provider": "google",
  "idToken": "<Google credential JWT from google.accounts.id.initialize>"
}
```

**Success — `200 OK`**
```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "email": "user@example.com"
}
```

**Failure — `401 Unauthorized`**
```json
{
  "code": 401,
  "name": "GOOGLE_TOKEN_INVALID",
  "message": "Google token is expired or has an invalid signature"
}
```

---

### POST `/api/auth/logout`

Clears the auth cookie. No request body needed.

**Success — `200 OK`** (empty body)

---

### GET `/api/auth/me`

Returns the currently authenticated user. Use this on app load to rehydrate auth state.

**Success — `200 OK`**
```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "email": "user@example.com"
}
```

**Failure — `401 Unauthorized`** (no valid cookie)

---

## Google OAuth Setup

**Client ID (dev):** `792777166754-sn1bgl9c1h4o43cit76pps1r83h4joej.apps.googleusercontent.com`

Use the **Google Identity Services** library (GSI) — not the deprecated Google Sign-In platform.js.

### 1. Load the GSI script

```html
<script src="https://accounts.google.com/gsi/client" async></script>
```

### 2. Initialize and render the button

```tsx
useEffect(() => {
  google.accounts.id.initialize({
    client_id: "792777166754-sn1bgl9c1h4o43cit76pps1r83h4joej.apps.googleusercontent.com",
    callback: handleGoogleCredential,
  });

  google.accounts.id.renderButton(
    document.getElementById("google-signin-btn"),
    { theme: "outline", size: "large" }
  );
}, []);
```

### 3. Send the credential to the API

```tsx
async function handleGoogleCredential(response: google.accounts.id.CredentialResponse) {
  const res = await fetch("/api/auth/external-login", {
    method: "POST",
    credentials: "include",         // required — sends/receives cookie
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      provider: "google",
      idToken: response.credential, // the JWT from Google
    }),
  });

  if (!res.ok) {
    const err = await res.json();
    // err.name === "GOOGLE_TOKEN_INVALID" | "GOOGLE_AUTH_FAILED" | "GOOGLE_AUTH_ERROR"
    return;
  }

  const user = await res.json(); // { userId, email }
  // store user in state / context
}
```

---

## Email Auth — Fetch Examples

```tsx
// Register
const res = await fetch("/api/auth/register", {
  method: "POST",
  credentials: "include",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify({ email, password }),
});

// Login
const res = await fetch("/api/auth/login", {
  method: "POST",
  credentials: "include",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify({ email, password }),
});

// Logout
await fetch("/api/auth/logout", { method: "POST", credentials: "include" });

// Get current user (on app load)
const res = await fetch("/api/auth/me", { credentials: "include" });
if (res.status === 401) { /* not logged in */ }
```

---

## Error Response Shape

All failure responses follow this structure:

```ts
type ErrorResponse = {
  code: number;    // mirrors HTTP status
  name: string;   // machine-readable error code
  message: string;
};
```

| Endpoint | Status | `name` |
|----------|--------|--------|
| `/login` | 401 | `INVALID_CREDENTIALS` |
| `/register` | 409 | `EMAIL_TAKEN` |
| `/external-login` | 401 | `GOOGLE_TOKEN_INVALID` \| `GOOGLE_AUTH_FAILED` \| `GOOGLE_AUTH_ERROR` |
| Any protected route | 401 | *(no body — cookie missing/expired)* |

---

## Cookie Details

| Property | Value |
|----------|-------|
| Name | `__Host-Nexus-Auth` |
| HttpOnly | Yes — JS cannot read it |
| Secure | Yes — HTTPS only |
| SameSite | Strict |
| Expiry | 5 min (known issue — token is valid 2h, cookie expires early; fix pending) |

> **Known issue:** The cookie expires in 5 minutes but the JWT is valid for 2 hours. Until the refresh token is implemented, users may get logged out early. Re-calling `GET /api/auth/me` after a 401 won't help — the client must re-authenticate.

---

## Protected Routes

Every endpoint except `/api/auth/external-login` requires the cookie. A missing or expired cookie returns `401` with no body. Redirect the user to the login page on any `401` from a non-auth endpoint.
