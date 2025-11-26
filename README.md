# Flow CMS

## A headless content management system.

![Status: Work in Progress](https://img.shields.io/badge/Status-Work%20in%20Progress-yellow)

## Important

This project is **actively under development**. The API structure, endpoints, and features are subject to change.

## Prerequisites

- .NET 9.0 SDK or later
- PostgreSQL

## Setup

1. **Clone the Repository:**
   ```bash
   git clone https://github.com/DamianKuras/flow-cms.git
   cd flow-cms
   ```

### Backend

1. **Create migration:**

```bash
dotnet ef migrations add Initial --project ./src/backend/Infrastructure
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

1. **Run Tests:**

```bash
dotnet test
```
