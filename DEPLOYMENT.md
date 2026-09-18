# Deploying TradingCardsAPI

The values in `appsettings.json` are development defaults. In production, override
them with environment variables (ASP.NET Core reads `__` as the section separator —
no code changes needed):

| Environment variable | Purpose |
|---|---|
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string, e.g. `Host=<host>;Port=5432;Database=TradingCardsDB;Username=<user>;Password=<pass>;SSL Mode=Require` |
| `Jwt__Key` | JWT signing secret. **Must** be changed from the committed dev value — generate 64+ random characters. Changing it logs every existing session out. |
| `PORT` | Injected by most hosting platforms (Render/Railway/Fly). The app binds to `http://0.0.0.0:$PORT` automatically when set. Otherwise set `ASPNETCORE_URLS`. |
| `ASPNETCORE_ENVIRONMENT` | Set to `Production` (disables Swagger UI). |

## Database

Migrations are applied with `dotnet ef database update` against the production
connection string, or run it once from your machine:

```powershell
$env:ConnectionStrings__DefaultConnection = "<production connection string>"
dotnet ef database update
```

## Outbound traffic

The API calls `api.scryfall.com` and `digimoncard.io` (card identification) and
serves image URLs from `cards.scryfall.io` / `images.digimoncard.io` to clients —
no keys required, but the server needs outbound HTTPS.

## Pointing the app at the server

No code change — pass the host at build time:

```
flutter build apk --dart-define=API_HOST=https://your-server.example.com
```

Without the flag it defaults to `http://localhost:5122` (USB + `adb reverse` dev flow).
Note: the Android release manifest does not allow cleartext HTTP, so the deployed
API must be reachable over HTTPS (any platform-provided TLS is fine).
