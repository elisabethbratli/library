# Home Library

Track, add and review your books.

- `backend/Library.Api` – .NET 9 minimal API + EF Core (PostgreSQL)
- `frontend` – React + Vite + TypeScript, served by nginx (which proxies `/api` to the API)
- `infra` – OpenTofu for Scaleway: container registry, Serverless Containers (api + web), Managed PostgreSQL on a private network

## Run locally
```bash
docker compose up --build   # http://localhost:8080
```
Or run pieces separately: `dotnet run --project backend/Library.Api --urls http://localhost:5080` and `cd frontend && npm run dev`.

## Deploy to Scaleway
```bash
export SCW_ACCESS_KEY=... SCW_SECRET_KEY=... SCW_DEFAULT_PROJECT_ID=...
./scripts/deploy.sh
```
The script applies the registry, builds and pushes both images, then applies the rest. The URL is printed as `web_url`.

Note: there is no authentication yet – anyone with the URL can edit your library.
