# Home Library

Track, add and review your books.

- `backend/Library.Api` – .NET 9 minimal API + EF Core (PostgreSQL)
- `frontend` – React + Vite + TypeScript, served by nginx (which proxies `/api` to the API)
- `infra` – OpenTofu for Scaleway: container registry, Serverless Containers (api + web), Managed PostgreSQL on a private network

## Run locally
```bash
docker compose up --build   # http://localhost:8080
```
Or run pieces separately (set `Auth__Password` for the API): `dotnet run --project backend/Library.Api --urls http://localhost:5080` and `cd frontend && npm run dev`.

## Deploy to Scaleway
```bash
export SCW_ACCESS_KEY=... SCW_SECRET_KEY=... SCW_DEFAULT_PROJECT_ID=...
./scripts/deploy.sh
```
The script applies the registry, builds and pushes both images, then applies the rest. The URL is printed as `web_url`.

## Authentication
One shared password protects the whole app. The API checks it at `POST /api/login` and sets an HttpOnly session cookie (30 days); every other `/api` route returns 401 without it. Login attempts are rate limited.

- Local: the password is `library` (see `docker-compose.yml`).
- Scaleway: a random password is generated. Read it with `tofu -chdir=infra output -raw app_password`.
- The API refuses to start if `Auth__Password` is not set.
