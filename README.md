# MedicBot

## Development Secrets Setup

You can use `dotnet user-secrets` or `appsettings.Development.json` for local development secrets.

- If a required secret is missing, startup logging will show the key as unresolved or the app may fail early during startup.

### dotnet user-secrets

The `UserSecretsId` is already included in [MedicBot/MedicBot.csproj](MedicBot/MedicBot.csproj), so you don't need to run `dotnet user-secrets init`. The values you set via `dotnet user-secrets` should override the placeholder values in `appsettings.Development.json` when your environment is set to Development.

Set the required secrets for the project:

```bash
dotnet user-secrets set "Discord:Token" "<discord-bot-token>" --project MedicBot/MedicBot.csproj
dotnet user-secrets set "Discord:OAuthClientId" "<discord-oauth-client-id>" --project MedicBot/MedicBot.csproj
dotnet user-secrets set "Discord:OAuthClientSecret" "<discord-oauth-client-secret>" --project MedicBot/MedicBot.csproj
dotnet user-secrets set "Auth:JwtSecret" "<base64-jwt-secret>" --project MedicBot/MedicBot.csproj
dotnet user-secrets set "MongoDb:ConnectionString" "<mongodb-connection-string>" --project MedicBot/MedicBot.csproj
```

### appsettings.Development.json

`appsettings.Development.json` already contains placeholder values set to: `SET_VIA_USER_SECRETS`. Simply replace these values with the actual secrets.

- Do not commit actual secret values to git.
