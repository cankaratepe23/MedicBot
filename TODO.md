# MedicBot TODO

## Code Quality Improvements

### High Priority

- [ ] **Global exception handler middleware** — Replace per-method try/catch in controllers with centralized exception handling middleware. Map domain exceptions to proper HTTP status codes (e.g., `AudioTrackNotFoundException` → 404, `UnauthorizedException` → 403) instead of returning `BadRequest(e.Message)` for everything.
- [ ] **Move secrets out of source code** — Lavalink password is hardcoded in `Constants.cs`. Move to configuration/environment variables or a secrets manager. Same for hardcoded guild IDs.
- [ ] **Fix thread safety in AudioManager** — `_lastPlayedTracks` is a `Dictionary<ulong, Queue<AudioTrack>>` accessed concurrently across guilds. Replace with `ConcurrentDictionary`.

### Medium Priority

- [ ] **Add unit test project** — No tests exist. Add a test project with at least service-layer unit tests for managers and repositories.
- [ ] **Abstract file I/O** — `AudioManager` and `ImageManager` directly call `File.OpenWrite`, `File.Delete`, `Directory.CreateDirectory`. Introduce a file system abstraction for testability.
- [ ] **Consistent async usage** — Some repositories use sync MongoDB operations (`InsertOne`, `Find().FirstOrDefault()`) while others are async. Standardize on async throughout.
- [ ] **Input validation layer** — Validation logic (filename checks, null checks) is scattered inline in managers. Consider a shared validation pattern or FluentValidation.

### Low Priority

- [ ] **Separate mute commands** — `AudioCommands` handles user muting, which belongs in a dedicated command module.
- [ ] **Type-safe settings** — Settings keys are string constants with `GetValue<int>()` calls. Consider a strongly-typed settings model.
- [ ] **Remove static `Program.Cleanup()`** — `_client` is still a static field; `BaseCommands.ShutdownBotCommand` calls `Environment.Exit(0)`. Migrate to proper lifecycle management.

## Security & Reliability

### High Priority

- [ ] **Path traversal protection** — `AudioManager.AddAsync()` and `ImageManager.AddAsync()` construct file paths from user-supplied names using string concatenation. Validate with `Path.GetFullPath()` that resolved paths remain within allowed directories (`res/`, `img/`).
- [ ] **File download size limits** — `AudioManager.AddAsync()`, `ImageManager.AddAsync()`, and `ImportExportManager.Import()` download files from URLs with no size cap. Implement max content-length checks to prevent disk exhaustion.
- [ ] **Unhandled null references in `MiscManager`** — `GetSelcukSportsUrlAsync()` calls `SelectSingleNode("//div/div/a[1]").Attributes["href"].Value` without null checks. HTML structure changes will cause `NullReferenceException`.

### Medium Priority

- [ ] **Add rate limiting** — No rate limiting on API endpoints. Implement `AspNetCore.RateLimiting` middleware with per-user limits to prevent abuse.
- [ ] **CancellationToken propagation** — Async methods in `AudioManager`, `ImageManager`, and `ImportExportManager` don't accept `CancellationToken`. Graceful shutdown could hang on long-running downloads or DB operations.
- [ ] **CORS origins in configuration** — Allowed origins (`medicbot.comaristan.com`, `127.0.0.1:3000`) are hardcoded in `Program.cs`. Move to `appsettings.json` per environment.
- [ ] **Hardcoded redirect URIs in `AuthController`** — `Login()` and `LocalLogin()` hardcode redirect URLs. Move to configuration.

## API Design

### Medium Priority

- [ ] **API versioning** — Controllers have no version prefix (e.g., `api/v1/`). Add route versioning to avoid breaking clients on future changes.
- [ ] **Consistent DTO usage** — Some endpoints return domain entities (`AudioTrack`) directly while others use DTOs. Always return DTOs to decouple internal models from API contracts.
- [ ] **Missing `[ProducesResponseType]` attributes** — Endpoints lack explicit response type declarations. Add them for accurate Swagger documentation.
- [ ] **Duplicate `GetCurrentUserId()` helper** — Both `AudioController` and `UserController` define identical private `GetCurrentUserId()` methods. Extract to a shared base controller or extension method.

## Performance

### Medium Priority

- [ ] **`VoiceStateHandler.TrackerUserAddPoints()` is O(n×m)** — Iterates all channels × all users to find a single user. Add a reverse lookup `Dictionary<ulong, ulong>` (userId → channelId) for O(1) access.
- [ ] **Use `Random.Shared` instead of `new Random()`** — `ImageManager` creates new `Random` instances per call, which can produce poor randomness when called in rapid succession. Use `Random.Shared` (available since .NET 6).
- [ ] **`GetOrderedByModified()` / `GetOrderedByDate()` load full documents** — These repository methods fetch entire `AudioTrack` documents when only timestamps are needed. Use MongoDB projections for lighter queries.
- [ ] **`All()` loads entire collection into memory** — `AudioRepository.All()` calls `.ToEnumerable()` / `.ToList()` on the full collection. Use server-side filtering/pagination for large collections.

## Code Quality

### Medium Priority

- [ ] **Magic numbers** — Queue capacity `new Queue<AudioTrack>(10)` in `AudioManager`, `Take(50)` in playback log queries, `Math.Clamp(limit, 1, 30)` thresholds — extract to named constants or configuration.
- [ ] **Use typed/named `HttpClient` registrations** — `AudioManager`, `ImageManager`, `ImportExportManager`, and `MiscManager` all call `_httpClientFactory.CreateClient()` with no name. Use named or typed HttpClients for better configuration (timeouts, base addresses, retry policies).
- [ ] **`Newtonsoft.Json` mixed with `System.Text.Json`** — `ImportExportManager` uses `JsonConvert.DeserializeObject` (Newtonsoft) while `AuthController` uses `System.Text.Json`. Standardize on one serializer.
- [ ] **Dead/incomplete code** — `PlaybackHub.SendRecentPlay()` has a TODO comment ("Might be removed later") and `AuthController.DiscordLogin()` returns `Ok()` with no implementation. Clean up or complete.

### Low Priority

- [ ] **Abstract Discord client operations** — `AudioManager` and `ImageManager` depend directly on `DiscordClient` for guild/channel lookups. Introduce an `IDiscordGuildService` abstraction for testability.
- [ ] **`IDisposable` on `VoiceStateHandler`** — Holds long-lived `ConcurrentDictionary` state with no cleanup mechanism. Implement `IDisposable` to flush pending points on shutdown.
- [ ] **Missing repository pagination** — Endpoints like `GET /Audio` with no limit return all tracks. Add server-side pagination (`skip`/`take` parameters) to avoid unbounded response sizes.
- [ ] **Lavalink configuration uses `Constants` instead of options pattern** — `Constants.LavalinkPassword`, `LavalinkHttp`, `LavalinkWebSocket` should use `IOptions<LavalinkOptions>` for consistency with the rest of the config.

## Architecture Follow-ups

### IHostedService for Discord

Wrap the Discord client lifecycle (`ConnectAsync`, `DisconnectAsync`, event wiring) in an `IHostedService` / `BackgroundService`. This enables graceful shutdown and proper ASP.NET Core lifecycle management, removing the static `_client` field and `Task.Delay(-1)` pattern in `Program.cs`.

### DSharpPlus 5.x Migration

DSharpPlus v5 has native DI support via `DiscordClientBuilder` and significant breaking changes. Worth doing as a dedicated follow-up after the current DI cleanup is stable.

### Mediator Pattern for Events

`BotSettingHandler` → `VoiceStateHandler` coupling could use MediatR for decoupled event notification. Direct injection is sufficient for the current 2-handler setup — revisit if more handlers are added.

### Multi-Project Split

Currently a single-project solution. Consider splitting into `MedicBot.Core` (domain/interfaces), `MedicBot.Infrastructure` (repositories, external services), and `MedicBot.Api` (controllers, commands, Program.cs) if the codebase grows.

### CQRS / MediatR

Not needed at current scale, but could help separate read/write paths if command complexity increases.
