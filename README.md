# Home Library

View, add and review the books in your home library.

| Part | Tech |
|---|---|
| `backend/src/Library.Api` | .NET 9 minimal API, EF Core migrations, PostgreSQL |
| `backend/tests/Library.Api.Tests` | xUnit integration tests against a real PostgreSQL |
| `frontend` | React + Vite + TypeScript (Vitest tests), served by nginx, which proxies `/api` to the API |
| `infra` | OpenTofu for Scaleway: container registry, two Serverless Containers (api, web), Managed PostgreSQL on a private network |

## Run locally
```bash
docker compose up --build   # http://localhost:8080, password: library
```

## Tests
```bash
# backend: needs a PostgreSQL server where the user may create databases (do not include Database=)
LIBRARY_TEST_DB="Host=localhost;Username=postgres;Password=postgres" dotnet test backend/Library.sln

# frontend
cd frontend && npm ci && npm test
```

## Authentication
One shared password protects the whole app. `POST /api/login` checks it and sets an HttpOnly session cookie (30 days);
every other `/api` route returns 401 without it. Login is rate limited (10 attempts per minute, global).
The API refuses to start if `Auth__Password` is not set.

## Deploy to Scaleway
1. Create an API key in the Scaleway console (IAM > API keys) and export it:
   ```bash
   export SCW_ACCESS_KEY=... SCW_SECRET_KEY=... SCW_DEFAULT_PROJECT_ID=...
   ```
2. Deploy (needs `tofu` and `docker`):
   ```bash
   ./scripts/deploy.sh
   ```
   It creates the registry, builds and pushes both images, then applies the rest.
3. Get the URL and the generated login password:
   ```bash
   tofu -chdir=infra output web_url
   tofu -chdir=infra output -raw app_password
   ```

The database costs roughly 10-15 EUR/month; the containers scale to zero when idle (first request is slow).
The state file contains the generated passwords: see `infra/backend.tf.example` to keep it in Object Storage.
Remove everything with `tofu -chdir=infra destroy`.
