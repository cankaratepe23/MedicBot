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
