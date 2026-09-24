# Deploying TradingCardsAPI

The values in `appsettings.json` are development defaults. In production, override
them with environment variables (ASP.NET Core reads `__` as the section separator —
no code changes needed):

| Environment variable | Purpose |
|---|---|
| `DATABASE_URL` | Preferred on managed platforms. The `postgres://user:pass@host/db` URL a provider hands out when you attach a database; the app converts it to Npgsql's keyword format and enables TLS. On Render, add it from the database's **Internal Database URL**. |
| `ConnectionStrings__DefaultConnection` | Alternative to `DATABASE_URL`, in Npgsql keyword form: `Host=<host>;Port=5432;Database=TradingCardsDB;Username=<user>;Password=<pass>;SSL Mode=Require`. Used only when `DATABASE_URL` is unset. |
| `Jwt__Key` | JWT signing secret. **Must** be changed from the committed dev value — generate 64+ random characters. Changing it logs every existing session out. |
| `PORT` | Injected by most hosting platforms (Render/Railway/Fly). The app binds to `http://0.0.0.0:$PORT` automatically when set. Otherwise set `ASPNETCORE_URLS`. |
| `ASPNETCORE_ENVIRONMENT` | Set to `Production` (disables Swagger UI). |

## Database

Migrations run automatically at startup, so a newly provisioned database gets
its schema on the first deploy. If the database is unreachable the app logs
`Database unavailable or migrations failed` and exits, which shows up as a
failed deploy rather than silent 500s on every request.

`GET /health` reports whether the database is reachable:

```json
{ "status": "ok", "database": true }
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
