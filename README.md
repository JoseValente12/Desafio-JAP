# Desafio JAP: Vehicle Rental

Vehicle rental management app built for the Grupo JAP technical challenge.
It manages vehicles, customers and rental contracts, and gives a daily overview of the fleet.

**Stack:** ASP.NET Core MVC (.NET 10), EF Core (Code-First), SQL Server 2022, xUnit.

The user interface is in Portuguese; code, commits and documentation are in English.

## Features

### Vehicles

* List with live search (brand, model or licence plate), filter by availability (Available / Rented) and pagination.
* Create, edit and delete. A vehicle with contracts cannot be deleted, so the rental history is never lost.
* The status (available or rented) is calculated from the contracts, never stored.
* Licence plates are normalised (`aa-11-bb` is stored as `AA11BB`) and unique. The manufacture year goes from 1900 to the current year.

### Customers

* List with live search (name or email) and pagination.
* Create, edit and delete (blocked when the customer has contracts).
* Email is unique and case-insensitive; phone numbers are normalised to digits; the driving licence number is stored in upper case.

### Rental contracts

* Create a contract: only the vehicles that are free in the chosen period are offered, and an overlap with another contract of the same vehicle is rejected with a clear message (the end date is inclusive).
* Edit contracts that have not started yet, with the same rules. Contracts that are running, finished or cancelled are read-only, so the history stays reliable.
* Cancel a contract (it is never deleted). Cancelled contracts stay in the history and no longer block the vehicle.
* The status (Upcoming, Active, Finished, Cancelled) is calculated from the dates and the cancellation.
* List with live search (customer, vehicle or plate), filters by status and by period, and pagination.

### Home dashboard

* Fleet figures: vehicles, rented today, available, occupancy rate, active contracts and customers.
* Pickups and returns of the day, and the movements of the next three days.
* Fuel breakdown and average age of the fleet.

### Quality and security

* Business rules live in services, not in controllers or views. Each screen has its own ViewModel, so entities are never bound from the request (no overposting).
* Validation on the server is the source of truth; client-side validation is only for comfort. Two simultaneous requests that hit a unique index get a field error instead of a crash.
* Anti-forgery validation on every POST, a Content Security Policy without `unsafe-inline`, security headers, HTTPS redirection and HSTS outside Development.
* Parameterised queries only (LINQ), no raw HTML from user data, friendly 404 and 500 pages that never show a stack trace.
* Automated tests (xUnit, SQLite in memory, a fake clock) cover the services, the filters, the dashboard and the security headers.

## Requirements

* [.NET 10 SDK](https://dotnet.microsoft.com/download)
* [Docker](https://www.docker.com/products/docker-desktop/) (on Windows, Docker Desktop needs WSL 2)
* EF Core tool: `dotnet tool install --global dotnet-ef`

## Database

SQL Server 2022 in a Docker container (on Apple Silicon Macs, keep the `--platform` flag; it is not needed on x86):

```
docker run -d --name japdesafio-sqlserver \
  --platform linux/amd64 \
  --restart unless-stopped \
  -e "ACCEPT_EULA=Y" \
  -e "MSSQL_SA_PASSWORD=<YOUR_STRONG_PASSWORD>" \
  -p 1433:1433 \
  -v japdesafio-sqlserver-data:/var/opt/mssql \
  mcr.microsoft.com/mssql/server:2022-latest
```

On Windows PowerShell, use a backtick (```) instead of `\` for line breaks, and `$env:Name="value"` instead of `export`.
The password must be strong (at least 8 characters, with upper case, lower case, digits and symbols), otherwise the container stops right after starting.

## Configuration

The connection string is not stored in the repository. There are two ways to set it.

Option 1: user-secrets (development)

```
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=JapCarRental;User Id=sa;Password=<YOUR_STRONG_PASSWORD>;TrustServerCertificate=True" --project src/JapCarRental.Web
```

Option 2: environment variable

```
export ConnectionStrings__DefaultConnection="Server=localhost,1433;Database=JapCarRental;User Id=sa;Password=<YOUR_STRONG_PASSWORD>;TrustServerCertificate=True"
```

PowerShell:

```
$env:ConnectionStrings__DefaultConnection="Server=localhost,1433;Database=JapCarRental;User Id=sa;Password=<YOUR_STRONG_PASSWORD>;TrustServerCertificate=True"
```

If both are set, the environment variable wins. See `src/JapCarRental.Web/appsettings.Example.json` for the expected shape. The app fails at startup with a clear message if no connection string is found.

## Create the database

In the Development environment (the default with `dotnet run`), the app applies the migrations and loads demo data on startup, so you only need the SQL Server container and the connection string.

To apply the migrations manually instead:

```
dotnet ef database update --project src/JapCarRental.Web
```

## Run

```
dotnet run --project src/JapCarRental.Web
```

On first run in Development, the app creates the `JapCarRental` database and seeds demo data (10 vehicles, 8 customers, 8 rental contracts). The data is fictional. The seed only runs when the database is empty, so restarting the app does not duplicate anything.

Migrations and seeding are limited to Development on purpose. In production, migrations should be applied in a controlled deployment step.

## Tests

```
dotnet test
```

## Project structure

```
src/JapCarRental.Web     ASP.NET Core MVC app
  Controllers            Thin controllers: read the request, call a service, pick a view
  Services               Business rules, filters and the dashboard (no MVC types)
  Models                 Entities with validation in their constructors and update methods
  ViewModels             One per screen (forms and lists)
  Views                  Razor views, in Portuguese
  Data                   DbContext and demo data seed
  Migrations             EF Core migrations
  Middleware             Security headers (CSP and others)
  Extensions             Helpers between services and MVC
tests/JapCarRental.Tests xUnit tests
```

## Development notes

* Commits follow [Conventional Commits](https://www.conventionalcommits.org/) (`feat`, `fix`, `test`, `docs`, `refactor`, `chore`), written in English.
* Short feature branches, merged into `main` through pull requests with a merge commit.
* Code is in English, the user interface is in Portuguese.
* Compiler rules (`Nullable`, `TreatWarningsAsErrors`) are set in `Directory.Build.props`.
* Vehicle status (available or rented) is calculated from the contracts, never stored.

When the model changes, create a new migration:

```
dotnet ef migrations add <MigrationName> --project src/JapCarRental.Web
```
