# Desafio JAP: Vehicle Rental

Vehicle rental management app built for the Grupo JAP technical challenge.
Manages vehicles, customers and rental contracts.

**Stack:** ASP.NET Core MVC (.NET 10), EF Core (Code-First), SQL Server 2022.

> Work in progress. The data model and initial migration are done; services and pages are being added.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/products/docker-desktop/) (on Windows, Docker Desktop needs WSL 2)
- EF Core tool: `dotnet tool install --global dotnet-ef`

## Database

SQL Server 2022 in a Docker container (on Apple Silicon Macs, keep the `--platform` flag; it is not needed on x86):

```bash
docker run -d --name japdesafio-sqlserver \
  --platform linux/amd64 \
  --restart unless-stopped \
  -e "ACCEPT_EULA=Y" \
  -e "MSSQL_SA_PASSWORD=<YOUR_STRONG_PASSWORD>" \
  -p 1433:1433 \
  -v japdesafio-sqlserver-data:/var/opt/mssql \
  mcr.microsoft.com/mssql/server:2022-latest
```

> On Windows PowerShell, use a backtick (`` ` ``) instead of `\` for line breaks, and `$env:Name="value"` instead of `export`.

The password must be strong (at least 8 characters, with upper case, lower case, digits and symbols), otherwise the container stops right after starting.

## Configuration

The connection string is not stored in the repository. There are two ways to set it.

**Option 1: user-secrets (development)**

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=JapCarRental;User Id=sa;Password=<YOUR_STRONG_PASSWORD>;TrustServerCertificate=True" --project src/JapCarRental.Web
```

**Option 2: environment variable**

```bash
export ConnectionStrings__DefaultConnection="Server=localhost,1433;Database=JapCarRental;User Id=sa;Password=<YOUR_STRONG_PASSWORD>;TrustServerCertificate=True"
```

PowerShell:

```powershell
$env:ConnectionStrings__DefaultConnection="Server=localhost,1433;Database=JapCarRental;User Id=sa;Password=<YOUR_STRONG_PASSWORD>;TrustServerCertificate=True"
```

If both are set, the environment variable wins. See `src/JapCarRental.Web/appsettings.Example.json` for the expected shape. The app fails at startup with a clear message if no connection string is found.

## Create the database

In the Development environment (the default with `dotnet run`), the app applies the migrations and loads demo data on startup, so you only need the SQL Server container and the connection string.

To apply the migrations manually instead:

```bash
dotnet ef database update --project src/JapCarRental.Web
```

## Run

```bash
dotnet run --project src/JapCarRental.Web
```

On first run in Development, the app creates the `JapCarRental` database and seeds demo data (10 vehicles, 8 customers, 8 rental contracts). The data is fictional. The seed only runs when the database is empty, so restarting the app does not duplicate anything.

Migrations and seeding are limited to Development on purpose. In production, migrations should be applied in a controlled deployment step.

## Tests

```bash
dotnet test
```

## Project structure

```
src/JapCarRental.Web     ASP.NET Core MVC app (Models, Data, Migrations)
tests/JapCarRental.Tests xUnit tests
```

## Development notes

- Commits follow [Conventional Commits](https://www.conventionalcommits.org/) (`feat`, `fix`, `test`, `docs`, `refactor`, `chore`), written in English.
- Short feature branches, merged into `main` through pull requests with a merge commit.
- Code is in English, the user interface is in Portuguese.
- Compiler rules (`Nullable`, `TreatWarningsAsErrors`) are set in `Directory.Build.props`.
- Vehicle status (available or rented) is calculated from the contracts, never stored.

When the model changes, create a new migration:

```bash
dotnet ef migrations add <MigrationName> --project src/JapCarRental.Web
```