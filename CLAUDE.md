# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

AbsoluteCinema is a C# Avalonia UI desktop application for cinema report management. It integrates with CinemaWeb (a legacy cinema management system) to download, process, and preview various report types (weekly, monthly, quarterly). It also fetches movie metadata from the Poiskkino API.

## Build & Run

```bash
dotnet build AbsoluteCinema.csproj
dotnet run
```

Requires .NET 10.0 SDK. No test project exists.

## Architecture

**MVVM pattern** with dependency injection (Microsoft.Extensions.Hosting) and CommunityToolkit.Mvvm attributes.

**Data flow:** Views → ViewModels → Services → External systems (CinemaWeb HTTP, Poiskkino API)

Key layers:
- **Views/** — Avalonia XAML UI (`*.axaml` files)
- **ViewModels/** — MVVM viewmodels using `[ObservableProperty]`, `[RelayCommand]` source generators
- **Services/** — Business logic, organized by domain (Movies/, Reports/)
- **Models/** and **Dtos/** — Domain models and API data transfer objects
- **Configuration/** — Typed config classes bound from `appsettings.json`

**DI setup** lives in `Hosting.cs` as extension methods on `HostApplicationBuilder`. Services are registered with keyed DI (e.g., `"weeklyReport"`, `"monthlyReport"`).

**Report service hierarchy:**
- `ReportService` (abstract base) → `CompositeReportService` → Specific services (Weekly, Monthly, Quarterly)
- `CinemaWebAccessor` (base HTTP client with auth) → `ReportProvider` (downloads reports from CinemaWeb)

## Code Conventions

- Primary constructors for DI (C# 12+): `public sealed class FooService(...)`
- Record types for DTOs
- `[GeneratedRegex]` for compiled regex patterns
- Nullable reference types enabled throughout
- Async/await with CancellationToken propagation
- Event-driven progress reporting (`OnDownloadProgress`)

## External Integrations

- **CinemaWeb** (http://192.168.3.150): Form-based auth, ASP.NET ReportViewer for report generation
- **Poiskkino API** (https://api.poiskkino.dev/v1.4): Movie metadata, requires API token, cached locally in `%AppData%/CinemaControl/movies.json`

## Key Dependencies

- Avalonia 11.3.12 (Fluent theme, DataGrid, compiled bindings)
- CommunityToolkit.Mvvm 8.2.1
- ClosedXML (Excel), DocX (Word documents), AvaloniaPdfViewer (PDF preview)
- Serilog (rolling daily file logs in `logs/`)
