---
name: production-ready
description: Fix all security vulnerabilities, refactor code structure, and deploy MediCare+ online
status: backlog
created: 2026-03-26T18:15:32Z
---

# PRD: production-ready

## Executive Summary

MediCare+ is a working telemedicine platform (ASP.NET Core 8 + .NET MAUI + PostgreSQL + SignalR) with full appointment, chat, video call, and notification features. It currently runs only on a local network and contains critical security vulnerabilities. This PRD covers fixing all known issues and making the app production-deployable and accessible online.

## Problem Statement

The app cannot be shared with real users because:
1. **Hardcoded secrets**: DB password and JWT signing key are in plain-text `appsettings.json`
2. **SSL bypass**: The mobile client skips certificate validation — vulnerable to MITM attacks
3. **Overly permissive CORS**: AllowAll policy exposes the API to any origin
4. **No token refresh**: 7-day JWT with no refresh flow means users must re-login after expiry, and revocation is impossible
5. **Monolithic source files**: `Controllers.cs` and `OtherServices.cs` bundle all logic into single files, making maintenance and debugging very difficult
6. **Local-only deployment**: API URL is hardcoded to a LAN IP (`192.168.11.163`), making the app unusable outside the local network

## User Stories

### As a System Administrator
- I want secrets stored securely so that a leaked config file does not expose the database or auth system
  - **Acceptance**: No credentials in source files; app reads from env vars / secrets manager
- I want the API deployed to a public URL so any user can access the app from anywhere
  - **Acceptance**: API reachable at a stable HTTPS domain; mobile app connects successfully

### As a Doctor or Patient
- I want my session to stay valid with refresh tokens so I am not suddenly logged out mid-day
  - **Acceptance**: Access token expires in 15 min; refresh token auto-renews it transparently; logout invalidates both
- I want my data transmitted securely so it cannot be intercepted
  - **Acceptance**: All API communication over HTTPS with valid certificate; no SSL bypass in mobile

### As a Developer
- I want controller and service files split by domain so I can navigate and edit code without scrolling through 2000-line files
  - **Acceptance**: Each controller and service in its own file, one class per file
- I want CORS locked down to known origins so unauthorized frontends cannot call the API
  - **Acceptance**: CORS policy lists explicit allowed origins; wildcard removed

## Functional Requirements

### FR-1: Secrets Management
- Move DB connection string to environment variable `DATABASE_URL`
- Move JWT secret to environment variable `JWT_SECRET`
- Provide `.env.example` with placeholder values
- App must fail to start with a clear error if required env vars are missing

### FR-2: JWT Refresh Token Flow
- Shorten access token lifetime to 15 minutes
- Issue a refresh token (valid 30 days) alongside the access token on login/register
- New endpoint: `POST /api/auth/refresh` — accepts refresh token, returns new access token
- New endpoint: `POST /api/auth/logout` — invalidates refresh token
- Store refresh tokens in a new `RefreshTokens` DB table (token hash, userId, expiry, revoked flag)
- Mobile app auto-refreshes token when it receives a 401

### FR-3: HTTPS & SSL
- Configure Kestrel to serve HTTPS with a valid certificate (Let's Encrypt or self-signed for dev)
- Remove `HttpClientHandler` SSL validation bypass from mobile client
- Mobile app trusts the server certificate properly

### FR-4: CORS Hardening
- Replace `AllowAll` policy with explicit origin allowlist
- Dev policy: `http://localhost:*`
- Prod policy: read allowed origins from env var `ALLOWED_ORIGINS`

### FR-5: Code Refactoring
- Split `Controllers.cs` into individual files: `AuthController.cs`, `DoctorController.cs`, `AppointmentController.cs`, `PatientController.cs`, `NotificationController.cs`, `ChatController.cs`, `AdminController.cs`
- Split `OtherServices.cs` into: `DoctorService.cs`, `PatientService.cs`, `AdminService.cs`, `ChatService.cs`
- Each file has exactly one class; no logic changes, only structural reorganization

### FR-6: Online Deployment
- Deploy API to Railway.app (free tier, supports .NET + PostgreSQL)
- Configure PostgreSQL on Railway (managed DB)
- Set all env vars in Railway dashboard
- Update mobile app to read API base URL from a config file (not hardcoded)
- Provide `AppConfig.cs` with `ApiBaseUrl` driven by build config (Debug = local, Release = production URL)

## Non-Functional Requirements

- All changes must be backward-compatible with existing DB schema (no breaking migrations)
- App must compile and run on Android and Windows (existing MAUI targets)
- Deployment must be automatable (Railway GitHub integration for CI/CD)
- No secrets committed to git; `.gitignore` must exclude `.env`

## Success Criteria

- [ ] `git grep -r "Password\|SecretKey\|123456789" -- "*.json" "*.cs"` returns no hardcoded credentials
- [ ] Mobile app connects to the live online API URL successfully
- [ ] Login flow works end-to-end on a phone not on the local network
- [ ] Refresh token is issued on login and auto-refresh works in mobile
- [ ] Each controller and service is in its own file
- [ ] CORS rejects a request from an unlisted origin with 403
- [ ] SSL bypass code is removed from mobile client

## Constraints & Assumptions

- Deployment target: Railway.app (free tier — sufficient for dev/demo)
- No Docker required (Railway auto-builds .NET apps from source)
- PostgreSQL stays as the database (no migration to another DB)
- No UI/UX changes in this PRD — backend and infrastructure only
- .NET 8 SDK available in deployment environment

## Out of Scope

- Adding new features (video call improvements, new specialties, etc.)
- Push notification service (FCM/APNs) — local notifications stay as-is
- Multi-tenancy or multi-hospital support
- Load balancing / horizontal scaling
- Mobile app store publishing

## Dependencies

- Railway.app account (free)
- Custom domain (optional — Railway provides a subdomain)
- GitHub repository (needed for Railway CI/CD integration)
- `gh` CLI or GitHub web UI for repo creation
