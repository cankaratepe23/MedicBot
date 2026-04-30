# MedicBot

## Development Secrets Setup

You can configure secrets using any of the following methods (listed in priority order, highest first):

1. Environment variables (with `MedicBot_` prefix)
2. `dotnet user-secrets`
3. `appsettings.Development.json`
4. `appsettings.json`

If a required secret is missing or still set to the placeholder value, the application will throw an `InvalidOperationException` at startup with a message describing which key needs to be configured.

### dotnet user-secrets (recommended for local development)

The `UserSecretsId` is already included in [MedicBot/MedicBot.csproj](MedicBot/MedicBot.csproj), so you don't need to run `dotnet user-secrets init`. The values you set via `dotnet user-secrets` will override the placeholder values in `appsettings.Development.json` when your environment is set to `Development`.

Set the required secrets for the project:

```bash
dotnet user-secrets set "Discord:Token" "<discord-bot-token>" --project MedicBot/MedicBot.csproj
dotnet user-secrets set "Discord:OAuthClientId" "<discord-oauth-client-id>" --project MedicBot/MedicBot.csproj
dotnet user-secrets set "Discord:OAuthClientSecret" "<discord-oauth-client-secret>" --project MedicBot/MedicBot.csproj
dotnet user-secrets set "Auth:JwtSecret" "<base64-jwt-secret>" --project MedicBot/MedicBot.csproj
dotnet user-secrets set "MongoDb:ConnectionString" "<mongodb-connection-string>" --project MedicBot/MedicBot.csproj
```

### Environment variables (recommended for production / CI)

The application registers a prefixed environment variable source with the prefix `MedicBot_`. Use `__` (double underscore) as the section separator.

| Configuration Key          | Environment Variable                    |
|----------------------------|-----------------------------------------|
| `Discord:Token`            | `MedicBot_Discord__Token`               |
| `Discord:OAuthClientId`    | `MedicBot_Discord__OAuthClientId`       |
| `Discord:OAuthClientSecret`| `MedicBot_Discord__OAuthClientSecret`   |
| `Auth:JwtSecret`           | `MedicBot_Auth__JwtSecret`              |
| `MongoDb:ConnectionString` | `MedicBot_MongoDb__ConnectionString`    |

Example (bash):

```bash
export MedicBot_Discord__Token="your-bot-token"
export MedicBot_Discord__OAuthClientId="your-client-id"
export MedicBot_Discord__OAuthClientSecret="your-client-secret"
export MedicBot_Auth__JwtSecret="your-base64-jwt-secret"
export MedicBot_MongoDb__ConnectionString="mongodb://..."
```

Example (PowerShell):

```powershell
$env:MedicBot_Discord__Token = "your-bot-token"
$env:MedicBot_Discord__OAuthClientId = "your-client-id"
$env:MedicBot_Discord__OAuthClientSecret = "your-client-secret"
$env:MedicBot_Auth__JwtSecret = "your-base64-jwt-secret"
$env:MedicBot_MongoDb__ConnectionString = "mongodb://..."
```

These can also be set in Docker (`--env` / `env_file`), systemd (`Environment=`), or any other deployment mechanism.

### appsettings.Development.json

`appsettings.Development.json` contains placeholder values set to `SET_VIA_USER_SECRETS`. You can replace these with actual values for local development, but **do not commit actual secrets to git**.
