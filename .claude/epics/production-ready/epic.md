---
name: production-ready
status: backlog
created: 2026-03-26T18:15:32Z
updated: 2026-03-26T18:15:32Z
progress: 0%
prd: .claude/prds/production-ready.md
github: (will be set on sync)
---

# Epic: production-ready

## Overview

Transform MediCare+ from a local-only dev prototype into a secure, production-deployable telemedicine platform. Six concrete work streams: secrets management, JWT refresh tokens, SSL/HTTPS, CORS hardening, code refactoring, and online deployment to Railway.app.

## Architecture Decisions

1. **Secrets via Environment Variables** — No secrets vault needed for this scale. Env vars in Railway dashboard are sufficient and keep the approach simple.
2. **Refresh Tokens in DB** — Store hashed refresh tokens in a new `RefreshTokens` table. Simple, auditable, revocable. No Redis needed.
3. **Railway.app for Hosting** — Supports .NET 8, managed PostgreSQL, free tier, GitHub CI/CD. Zero infrastructure setup.
4. **AppConfig.cs for Mobile URLs** — A single config class driven by `#if DEBUG` preprocessor directive. Simple, no extra DI needed.
5. **File-per-class Refactor** — Pure structural change: copy-paste classes into own files, delete originals. Zero logic changes = zero regression risk.

## Technical Approach

### Backend Services
- Add `RefreshTokens` DbSet and migration
- `AuthService`: issue + validate + revoke refresh tokens
- New endpoints: `POST /api/auth/refresh`, `POST /api/auth/logout`
- `Program.cs`: read secrets from env vars, configure CORS with allowlist, configure HTTPS

### Frontend (Mobile)
- Remove `HttpClientHandler` with `ServerCertificateCustomValidationCallback`
- Add `AppConfig.cs` with `ApiBaseUrl` conditional on build
- Update `ApiService.cs` to use `AppConfig.ApiBaseUrl`
- Add 401 interceptor to call refresh endpoint and retry

### Infrastructure
- `appsettings.json`: replace hardcoded values with env var references
- `appsettings.Production.json`: production-specific config
- `.env.example`: document required env vars
- `.gitignore`: ensure `.env` is excluded
- Railway: create project, link GitHub repo, provision PostgreSQL, set env vars

## Implementation Strategy

Tasks are ordered by dependency:
1. **Refactor first** (no logic changes, establishes clean file structure for subsequent edits)
2. **Secrets management** (must come before deployment)
3. **JWT refresh tokens** (auth changes before CORS/SSL)
4. **CORS hardening** (after secrets, reads from env var)
5. **SSL / HTTPS** (after secrets)
6. **Online deployment** (last — all fixes must be in place)

Tasks 1, 2, 3, 4, 5 can largely run in parallel after task 1 (refactor) completes.

## Task Breakdown Preview

| # | Task | Depends On | Parallel? |
|---|------|-----------|-----------|
| 1 | Refactor monolithic files into per-class files | — | No (establishes base) |
| 2 | Secrets management (env vars) | 1 | Yes |
| 3 | JWT refresh token flow | 1 | Yes |
| 4 | CORS hardening | 2 | Yes |
| 5 | SSL/HTTPS + remove mobile SSL bypass | 2 | Yes |
| 6 | Online deployment to Railway.app | 2,3,4,5 | No (final integration) |

## Dependencies

- Railway.app account
- GitHub repository for the project
- .NET 8 SDK (already in use)
- EF Core migration tooling (already installed)

## Success Criteria (Technical)

- `dotnet build` passes with zero warnings on both projects
- `POST /api/auth/login` returns `{ accessToken, refreshToken }`
- `POST /api/auth/refresh` with valid refresh token returns new access token
- `POST /api/auth/refresh` with expired/revoked token returns 401
- CORS returns 403 for unlisted origins
- Mobile app builds in Release config pointing to Railway URL
- API reachable at `https://<app>.railway.app` from the public internet
- No credentials in any `.cs` or `.json` source file

## Estimated Effort

- Task 1 (Refactor): ~2h — mechanical, low risk
- Task 2 (Secrets): ~1h — config changes
- Task 3 (JWT Refresh): ~3h — new DB table, service logic, endpoints, mobile interceptor
- Task 4 (CORS): ~30min — config change
- Task 5 (SSL): ~1h — Kestrel config + mobile fix
- Task 6 (Deploy): ~2h — Railway setup, env vars, test end-to-end

## Tasks Created
- [ ] 001.md - Refactor monolithic files into per-class files (parallel: false)
- [ ] 002.md - Secrets management via environment variables (parallel: true)
- [ ] 003.md - JWT refresh token flow (parallel: true)
- [ ] 004.md - CORS hardening (parallel: true)
- [ ] 005.md - HTTPS configuration and remove mobile SSL bypass (parallel: true)
- [ ] 006.md - Online deployment to Railway.app (parallel: false)

Total tasks: 6
Parallel tasks: 4 (002, 003, 004, 005)
Sequential tasks: 2 (001 first, 006 last)
Estimated total effort: ~9.5 hours
