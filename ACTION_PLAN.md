# MedicBot Action Plan

This plan turns the current review into a small number of ordered workstreams.

The goal is not to polish everything. The goal is to remove the highest-risk issues first, then add the minimum proof that the project is reliable, maintainable, and intentionally designed.

## Priority Order

1. Lock down security and correctness
2. Add tests and CI gates
3. Make persistence and runtime behavior intentional
4. Replace the deployment story with an immutable release flow
5. Document the architecture and tradeoffs

## Phase 1: Security And Correctness

Target: one focused weekend

### 1. Protect privileged bot-control endpoints

Current anchors:
- `MedicBot/Controller/AudioController.cs`
- `MedicBot/Program.cs`

Tasks:
- Add an admin-only authorization policy for bot control operations.
- Require authentication on `JoinGuild`, `JoinChannel`, and `Leave`.
- Verify the caller is allowed to control the target guild, not just authenticated.
- Add rate limiting for sensitive API routes.
- Remove or narrow any HTTP surface that only exists for owner/operator workflows.

Done when:
- Anonymous requests to bot-control endpoints are rejected.
- Authenticated non-admin users cannot move the bot.
- Sensitive routes have explicit authorization and rate limits.

### 2. Tighten the Discord auth flow

Current anchors:
- `MedicBot/Controller/AuthController.cs`
- `MedicBot/Manager/TokenManager.cs`
- `MedicBot/Program.cs`

Tasks:
- Stop accepting caller-supplied OAuth client identity as trusted input.
- Bind Discord client id, secret, and redirect URIs from configuration only.
- Remove the token-bridging path unless there is a hard requirement for it.
- Validate the OAuth exchange against the configured Discord app only.
- Keep refresh-token hashing as-is, but add explicit rotation and revocation tests.

Done when:
- The backend only issues local tokens after a server-controlled OAuth exchange.
- Redirect URIs and client ids are not accepted from request bodies.
- Refresh-token rotation is covered by tests.

### 3. Move remaining secrets and runtime settings out of source

Current anchors:
- `MedicBot/Utils/Constants.cs`
- `MedicBot/Options/`
- `MedicBot/appsettings.json`
- `MedicBot/appsettings.Development.json`

Tasks:
- Move Lavalink password, host, websocket URI, owner id, and guild allowlist into typed options.
- Validate those options at startup the same way JWT and Mongo settings are validated.
- Keep constants only for true constants, not environment-specific runtime values.

Done when:
- No secrets or environment-specific identifiers remain hardcoded in `Constants.cs`.
- Startup fails fast when required runtime configuration is missing.

### 4. Fix async correctness on write paths

Current anchors:
- `MedicBot/Manager/UserManager.cs`
- `MedicBot/Repository/UserMuteRepository.cs`
- `MedicBot/Repository/UserFavoritesRepository.cs`
- `MedicBot/Controller/UserController.cs`

Tasks:
- Change fire-and-forget repository calls to awaited calls.
- Make mutating `IUserManager` operations async where needed.
- Propagate `Task` through controllers and command handlers.
- Add tests that prove writes have completed before handlers return.

Done when:
- No repository write is silently ignored.
- Mutation methods either complete synchronously by design or are properly awaited.

### 5. Replace broad exception handling with one error pipeline

Current anchors:
- `MedicBot/Controller/AudioController.cs`
- `MedicBot/Commands/AudioCommands.cs`
- `MedicBot/Commands/ImageCommands.cs`

Tasks:
- Add centralized exception handling middleware for HTTP.
- Map domain exceptions to specific status codes and `ProblemDetails`.
- Stop returning raw exception messages from catch-all blocks.
- Narrow Discord command exception handling to expected business exceptions.

Done when:
- Controllers no longer wrap most actions in `catch (Exception)`.
- HTTP responses are consistent and predictable for known failure modes.

## Phase 2: Tests And CI Gates

Target: one focused weekend

### 6. Add a real test project

Current anchors:
- `MedicBot.sln`
- `MedicBot/MedicBot.csproj`
- `MedicBot/Manager/`
- `MedicBot/Repository/`

Tasks:
- Add `MedicBot.Tests` to the solution.
- Add unit tests for:
  - token generation and refresh-token rotation
  - pricing rules in `AudioTrack` helpers
  - authorization decisions in audio deletion and playback
  - exception mapping middleware
- Add integration tests using Testcontainers for MongoDB for:
  - refresh-token persistence and lookup
  - favorites uniqueness behavior
  - settings initialization or index initialization

Done when:
- The repo has an executable test suite, not just manual verification.
- Core business rules and failure paths are covered.

### 7. Make CI fail on broken code

Current anchors:
- `.github/workflows/main.yml`

Tasks:
- Split workflow into `build`, `test`, `publish`, and `deploy` jobs.
- Run restore, build, and tests on every push and pull request.
- Make deploy depend on successful build and test jobs.
- Add analyzer enforcement if warnings are promoted or configured.

Done when:
- No deployment can happen if build or tests fail.
- CI produces a clear signal for both code health and release readiness.

## Phase 3: Data Layer And Runtime Hardening

Target: one weekend, or split across smaller sessions

### 8. Add an explicit MongoDB index strategy

Current anchors:
- `MedicBot/Repository/AudioRepository.cs`
- `MedicBot/Repository/UserFavoritesRepository.cs`
- `MedicBot/Repository/RefreshTokenRepository.cs`
- `MedicBot/Repository/SettingsRepository.cs`
- `MedicBot/Repository/AudioPlaybackLogRepository.cs`

Tasks:
- Add a startup index initializer.
- Create at least these indexes:
  - unique `AudioTrack.Name`
  - unique compound `UserFavorite(UserId, TrackId)`
  - unique `RefreshToken.TokenHash`
  - TTL on `RefreshToken.ExpiresAt`
  - unique `BotSetting.Key`
  - playback-log indexes on `UserId` and `Timestamp`
- Document the indexes in the README or an ADR.

Done when:
- Index creation is explicit, repeatable, and part of startup.
- Query shape and indexes match each other.

### 9. Stop loading full logs into memory for usage stats

Current anchors:
- `MedicBot/Manager/AudioManager.cs`
- `MedicBot/Repository/AudioPlaybackLogRepository.cs`

Tasks:
- Replace `GetGlobalLog()` plus in-memory grouping with aggregation pipelines.
- Project only the fields needed for recent and frequent track views.
- Add tests around the aggregation results.

Done when:
- Track recents and frequency queries are computed server-side.
- The app no longer depends on scanning the full playback log in memory.

### 10. Make the runtime behave like a hosted service

Current anchors:
- `MedicBot/Program.cs`
- `MedicBot/EventHandler/VoiceStateHandler.cs`

Tasks:
- Move Discord connection lifecycle into `IHostedService` or `BackgroundService`.
- Remove the static `_client` lifecycle pattern and `Task.Delay(-1)` wait loop.
- Replace `async void` shutdown callback with awaited shutdown behavior.
- Add `CancellationToken` support to long-running async paths.

Done when:
- Startup and shutdown are managed by ASP.NET Core hosting.
- The application can stop cleanly without ad hoc static state.

### 11. Add baseline operability

Current anchors:
- `MedicBot/Program.cs`
- `MedicBot/Controller/AuthController.cs`
- `MedicBot/Manager/AudioManager.cs`
- `MedicBot/Manager/ImageManager.cs`
- `MedicBot/Manager/ImportExportManager.cs`

Tasks:
- Add health checks for API readiness and MongoDB connectivity.
- Add named or typed `HttpClient` registrations with timeouts.
- Add retry policies only where retry is safe.
- Add request correlation to logs.
- Add a small metrics surface or tracing baseline.

Done when:
- The app has a health endpoint.
- Outbound HTTP is centrally configured.
- Logs can be tied back to a request or operation.

## Phase 4: Deployment

Target: one weekend

### 12. Replace `git pull` deployment with immutable releases

Current anchors:
- `.github/workflows/main.yml`
- deployment scripts or VPS service definitions outside this repo

Tasks:
- Add a multi-stage Dockerfile.
- Build and publish a versioned image tagged with the commit SHA.
- Push images to GHCR or another registry.
- Change the VPS deploy step to pull and run that image.
- Keep the previous image available for rollback.
- Fail deployment if health checks do not pass.

Done when:
- Deployment uses an immutable artifact.
- Rollback means re-running the previous image, not another `git pull`.

## Phase 5: Documentation

Target: a few hours

### 13. Rewrite the README around the actual system

Current anchors:
- `README.md`

Tasks:
- Add a short system overview: Discord bot, web API, MongoDB, realtime playback notifications, deployment model.
- Add local setup steps from clone to run.
- Add test instructions.
- Add deployment overview.
- Add a simple architecture diagram.

Done when:
- Someone can understand what the system is, how to run it, and where the main boundaries are.

### 14. Add ADRs for the non-obvious decisions

Suggested ADR topics:
- MongoDB schema and index choices
- Auth flow and token model
- Deployment model and rollback strategy

Done when:
- The project explains why the design is the way it is, not just what files exist.

## Minimum Useful Cut

If time is limited, do these first:

1. Protect bot-control endpoints and tighten auth flow.
2. Remove hardcoded secrets and runtime identifiers from source.
3. Fix ignored async writes.
4. Add a test project with the first useful unit and integration tests.
5. Make CI run build and tests before deploy.
6. Replace `git pull` deployment with versioned image deployment.
7. Rewrite the README once the above is done.

## Nice-To-Have Follow-Ups

These are useful, but they should come after the work above:

- Split the solution into smaller projects if the current boundaries keep growing.
- Add a file-system abstraction around direct file I/O for better testing.
- Revisit MediatR or another command/handler structure if feature complexity keeps increasing.
- Consider moving some string-key settings into strongly typed options where that improves safety.

## Progress Tracking Suggestion

When work starts, turn each numbered item above into a tracked issue or PR-sized checklist.

Recommended cadence:
- one PR for Phase 1 security and correctness
- one PR for tests and CI
- one PR for MongoDB plus operability
- one PR for deployment
- one PR for documentation