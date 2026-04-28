# FilesXchange

FilesXchange is a lightweight file-sharing service without authentication. A user uploads one or more files, receives a token, and then shares a download link built from that token.

## Docker Run

Build and start the application:

```bash
docker compose up -d --build
```

Open:

```text
http://localhost
```

Check containers:

```bash
docker compose ps
```

Stop containers:

```bash
docker compose down
```

Stop containers and remove uploaded files, logs, and SQLite data volumes:

```bash
docker compose down -v
```

## Local Development

Requirements:

```text
.NET 10 SDK
Node.js 24 or newer
npm 11 or newer
Bash
```

Install frontend dependencies once:

```bash
cd src/FilesXchange.Client
npm install
```

Start backend and frontend together:

```bash
./self-host/dev-local.sh
```

If the script is not executable after copying the project, run:

```bash
chmod +x self-host/dev-local.sh
```

Local URLs:

```text
Frontend: http://localhost:5173
Backend:  http://localhost:5299
Health:   http://localhost:5299/health
```

In local development, Vite proxies `/api` requests to `http://localhost:5299`.

## Environment Variables

The backend reads configuration from `appsettings.json`, `appsettings.Development.json`, and environment variables. Environment variables override JSON settings.

Common backend variables:

```text
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://+:8080
ConnectionStrings__Default=Data Source=filesxchange.db
FilesXchange__MaxFileSizeBytes=2147483648
FilesXchange__MaxFilesPerUpload=100
FilesXchange__MaxFileNameLength=128
FilesXchange__TokenExpirationDays=7
FilesXchange__CleanupIntervalSeconds=300
FilesXchange__UploadDirectory=uploads
FilesXchange__LogDirectory=logs
```

Docker image defaults:

```text
ASPNETCORE_URLS=http://+:8080
FilesXchange__UploadDirectory=/app/uploads
FilesXchange__LogDirectory=/app/logs
ConnectionStrings__Default=Data Source=/app/data/filesxchange.db
```

The frontend currently does not require environment variables. In Docker, nginx proxies `/api` to the backend service. In local development, Vite handles the same `/api` proxy.

## Tests

Backend tests:

```bash
dotnet test
```

Frontend tests:

```bash
cd src/FilesXchange.Client
npm run test
```

Frontend production build:

```bash
cd src/FilesXchange.Client
npm run build
```
