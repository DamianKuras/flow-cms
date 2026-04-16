# Flow CMS

## A headless content management system.

![Status: Work in Progress](https://img.shields.io/badge/Status-Work%20in%20Progress-yellow)

## Important

This project is **actively under development**. The API structure, endpoints, and features are subject to change.

## Prerequisites

- .NET 10.0 SDK
- Node.js / npm
- Docker Desktop

---

## Running with Docker

The easiest way to run the full stack locally:

```bash
cp .env.example .env   # fill in your values
docker compose up --build
```

| Service  | URL                   |
| -------- | --------------------- |
| Frontend | http://localhost:5173 |
| Backend  | http://localhost:5043 |

Default seed credentials (from `src/backend/Api/appsettings.Development.json`):

| Email             | Password    |
| ----------------- | ----------- |
| `admin@admin.com` | `Admin@123` |

To override seed credentials, set environment variables in `.env`:

| Variable                | Description                                    |
| ----------------------- | ---------------------------------------------- |
| `Seed__AdminEmail`      | Admin account email                            |
| `Seed__AdminPassword`   | Admin account password                         |
| `Seed__DevUserPassword` | Dev user password (Development only)           |
| `Seed__DevUserCount`    | Number of dev users to seed (Development only) |

---

## Local Development (without Docker)

### Backend

1. **Configure local settings** — `appsettings.Development.json` ships with working defaults.

2. **Run migrations:**

   ```bash
   dotnet ef database update --project ./src/backend/Infrastructure --startup-project ./src/backend/Api
   ```

3. **Start the API:**

   ```bash
   dotnet run --project ./src/backend/Api
   ```

### Frontend

```bash
cd src/frontend
npm install
npm run dev
```

---

## Tests

### Unit tests (no Docker required)

```bash
dotnet test ./tests/Domain.Tests
dotnet test ./tests/Application.Tests
```

### Integration tests (requires Docker)

Uses Testcontainers to spin up a real PostgreSQL container.

```bash
dotnet test ./tests/Integration.Tests
```

### E2E tests (requires Docker)

Full-stack browser tests: Playwright drives a headless Chromium browser against the full stack running in Docker Compose.

#### Option A — fully in Docker (no local setup needed)

```bash
docker compose -f docker-compose.e2e.yml run --build --rm tests
```

Starts db + backend + frontend, then runs a `tests` container built from the official Playwright .NET image (browsers pre-installed). No separate browser install step.

#### Option B — local test runner

Requires Playwright browser binaries installed once:

```bash
dotnet build tests/E2E.Tests
powershell tests\E2E.Tests\bin\Debug\net10.0\playwright.ps1 install chromium
```

Then:

```bash
dotnet test ./tests/E2E.Tests
```

The fixture automatically starts the Docker Compose stack, waits for readiness, runs tests, and tears down on completion.

To watch the browser during local test runs, set `Headless = false` in `E2ECollectionFixture.cs`.

### Frontend tests (Vitest)

```bash
cd src/frontend
npm test
```

### Code coverage

One-time install:

```bash
dotnet tool install -g dotnet-reportgenerator-globaltool
```

Generate report:

```bat
test_with_coverage.bat
```

---

## Content lifecycle

### Content types

A content type defines the schema (fields) that content items must conform to. Content types are versioned and move through the following states:

```
DRAFT → PUBLISHED → ARCHIVED
```

- **DRAFT** — the working schema. Editable. Not yet visible to consumers.
- **PUBLISHED** — the active schema. Publishing a draft creates a **new row** with an incremented version number. The previous published version is archived automatically.
- **ARCHIVED** — a retired version. Read-only, kept for historical reference.

Only one published version of a content type exists at a time. The `name` field is the stable identifier across all versions.

### Content items

A content item is a data record that conforms to a specific content type's schema. Content items follow the same versioning model:

```
Draft → Published
         ↑
   (previous published archived)
```

- **Draft** — the working copy. Editable. Not served to consumers.
- **Published** — the live version. Publishing a draft creates a **new row** with an incremented version. The previous published version is soft-deleted.

The `title` + `contentTypeId` pair is the stable identifier across versions. After publishing, the draft remains and can be edited to produce a future version.

### Publish flow (content items)

1. Create or edit a content item draft.
2. `POST /content-items/{draftId}/publish`
3. A new published row is created (new ID, `Version = prev + 1`).
4. The previous published version (if any) is soft-deleted.
5. The draft is preserved.
6. The response returns the ID of the new published item.

---

## Plugin system

Validation and transformation rules can be shipped as separate .NET assemblies. At startup, the API scans the `plugins/` folder for `.dll` files and registers any types implementing `IValidationRule` or `ITransformationRule`.

An example plugin lives at `src/backend/ExamplePlugin`. Build it and copy the `.dll` to the `plugins/` folder next to the compiled API.
