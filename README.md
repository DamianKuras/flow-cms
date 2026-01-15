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
