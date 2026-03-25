# Flow CMS

## A headless content management system.

![Status: Work in Progress](https://img.shields.io/badge/Status-Work%20in%20Progress-yellow)

## Important

This project is **actively under development**. The API structure, endpoints, and features are subject to change.

## Prerequisites

- .NET 9.0 SDK or later
- PostgreSQL
- **Docker Desktop** (Required only for Integration Tests via Testcontainers)

## Setup

1. **Clone the Repository:**
   ```bash
   git clone https://github.com/DamianKuras/flow-cms.git
   cd flow-cms
   ```

### Backend

1. **Create migration:**

```bash
dotnet ef migrations add Initial --project ./src/backend/Infrastructure --startup-project ./src/backend/Api

```

1. **Update Database:**

```bash
dotnet ef database update --project ./src/backend/Infrastructure --startup-project ./src/backend/Api
```

2. **Run Api:**

```bash
dotnet run --project ./src/backend/Api
```

### Frontend

1. **Install dependencies:**

   ```bash
   cd src/frontend
   npm install
   ```

2. **Start dev server:**

   ```bash
   npm run dev
   ```

### Frontend Tests

Frontend tests use [Vitest](https://vitest.dev/) with [Testing Library](https://testing-library.com/).

1. **Run all tests once:**

   ```bash
   cd src/frontend
   npm test
   ```

Tests live in `src/frontend/tests/`, mirroring the source structure (e.g. `tests/rules/validation/min-length-rule.test.tsx` tests `src/rules/validation/min-length-rule.tsx`).

---

### Backend Tests

This project contains both **Unit Tests** and **Integration Tests**.

#### ⚠️ Integration Tests Requirement

The **Integration.Tests** project uses **Testcontainers**. You must have **Docker** installed and running to execute these tests, as they spin up a real PostgreSQL container dynamically.

1. **Run All Tests (Requires Docker)**

   ```bash
   dotnet test
   ```

2. **Run Tests Without Docker (Unit Tests Only)**
   If you do not have Docker running, you can run specific unit test for projects individually for example domain tests:

   ```bash
   dotnet test ./tests/Domain.Tests
   ```

   or Application tests:

   ```bash
   dotnet test ./tests/Application.Tests
   ```

### Code Coverage

Generates a merged HTML coverage report from Domain, Application, and Integration tests.

#### Prerequisites — one-time install

```bash
dotnet tool install -g dotnet-reportgenerator-globaltool
```

#### Run

```bat
test_with_coverage.bat
```

The report opens automatically in your browser. Coverage is scoped to `Domain`, `Application`, `Infrastructure`, and `Api` assemblies — EF migrations and auto-generated OpenAPI code are excluded.

> **Note:** E2E tests are excluded from coverage collection because they start the API as a separate process, which cannot be instrumented.

---

### E2E Tests (Playwright .NET)

The `E2E.Tests` project runs full-stack browser tests: a real Chromium browser talks to the React frontend, which talks to an ASP.NET Core API backed by a PostgreSQL Testcontainers database — all started automatically by the test runner.

#### ⚠️ Requirements

- **Docker** — for the PostgreSQL Testcontainers database
- **Node.js / npm** — to start the Vite dev server
- **Playwright browsers** — one-time install (see below)

#### One-time: install Playwright browser binaries

After building the project, run:

```bash
dotnet build tests/E2E.Tests
```

Then install the browser binaries. Use whichever PowerShell is available:

```powershell
# Windows PowerShell (built-in)
powershell tests\E2E.Tests\bin\Debug\net10.0\playwright.ps1 install chromium

# PowerShell Core
pwsh tests/E2E.Tests/bin/Debug/net10.0/playwright.ps1 install chromium
```

#### Run E2E tests

```bash
dotnet test tests/E2E.Tests
```

The test fixture automatically:
1. Starts a PostgreSQL container and runs migrations
2. Starts the ASP.NET Core API on `http://localhost:5252`
3. Starts the Vite frontend on `http://localhost:5173` (using `.env.e2e` so `VITE_CMS_API_URL` points to the test API)
4. Launches a headless Chromium browser

#### Run E2E tests headed (visible browser)

To watch the browser during a test run, change `Headless = true` to `Headless = false` in `E2ECollectionFixture.cs`.

---

## Validation Rules and Transformation Rules Plugin Guide

This project supports **validation rule plugins** and **transformation rule plugins** implemented as separate .NET assemblies.  
Plugins are discovered and loaded at runtime by scanning a plugins folder for `.dll` files and reflecting on types that implement `IValidationRule` or `ITransformationRule`.

---

## How Plugin Loading Works

At startup, the host application:

1. Loads all `.dll` files from a configured `plugins` folder.
2. Scans each assembly for types implementing `IValidationRule` or `ITransformationRule`.
3. Registers them into the validation rule registry.
4. Makes them available for validation logic in your app.

Example plugin folder is located under /src/backend/ExamplePlugin
you can build this project and then move PluginExample.dll to the /plugins folder in your compiled cms app.
