FROM mcr.microsoft.com/playwright/dotnet:v1.51.0-noble

# The base image has .NET 8 and the Microsoft apt repo already configured.
# Install .NET 10 SDK alongside it.
RUN apt-get update && \
    apt-get install -y dotnet-sdk-10.0 && \
    rm -rf /var/lib/apt/lists/*

WORKDIR /repo

COPY flow-cms-backend.sln ./
COPY src/backend/Domain/Domain.csproj src/backend/Domain/
COPY src/backend/Application/Application.csproj src/backend/Application/
COPY src/backend/Infrastructure/Infrastructure.csproj src/backend/Infrastructure/
COPY src/backend/Api/Api.csproj src/backend/Api/
COPY tests/E2E.Tests/E2E.Tests.csproj tests/E2E.Tests/

RUN dotnet restore tests/E2E.Tests/E2E.Tests.csproj

COPY src/backend/ src/backend/
COPY tests/E2E.Tests/ tests/E2E.Tests/
COPY src/frontend/src/i18n/ src/frontend/src/i18n/

RUN dotnet build tests/E2E.Tests/E2E.Tests.csproj

ENTRYPOINT ["dotnet", "test", "tests/E2E.Tests", "--no-build", "--logger", "console;verbosity=normal"]
