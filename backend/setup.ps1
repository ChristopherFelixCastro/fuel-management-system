<#
.SYNOPSIS
    Scaffolds the FuelManagement .NET 8 backend solution.
    Run once from the backend/ directory:
        pwsh -File setup.ps1
#>

$ErrorActionPreference = "Stop"

# Resolve the backend root relative to this script
$b = Split-Path -Parent $MyInvocation.MyCommand.Definition
if (-not $b) { $b = Get-Location }

Write-Host "=== FuelManagement Backend Scaffold ===" -ForegroundColor Cyan
Write-Host "Target: $b`n"

# ---------------------------------------------------------------------------
# [1/7] Solution
# ---------------------------------------------------------------------------
Write-Host "[1/7] Solution..." -ForegroundColor Yellow
dotnet new sln -n FuelManagement -o $b --force

# ---------------------------------------------------------------------------
# [2/7] Projects
# ---------------------------------------------------------------------------
Write-Host "`n[2/7] Projects..." -ForegroundColor Yellow
dotnet new webapi --use-controllers -n FuelManagement.Api    `
    -o "$b\src\FuelManagement.Api"    -f net8.0 --force
dotnet new classlib -n FuelManagement.Shared   `
    -o "$b\src\FuelManagement.Shared"   -f net8.0 --force
dotnet new classlib -n FuelManagement.Closures `
    -o "$b\src\FuelManagement.Closures" -f net8.0 --force
dotnet new classlib -n FuelManagement.Alerts   `
    -o "$b\src\FuelManagement.Alerts"   -f net8.0 --force
dotnet new classlib -n FuelManagement.Reports  `
    -o "$b\src\FuelManagement.Reports"  -f net8.0 --force

# ---------------------------------------------------------------------------
# [3/7] Add to solution
# ---------------------------------------------------------------------------
Write-Host "`n[3/7] Adding projects to solution..." -ForegroundColor Yellow
dotnet sln "$b\FuelManagement.sln" add `
    "$b\src\FuelManagement.Api\FuelManagement.Api.csproj" `
    "$b\src\FuelManagement.Shared\FuelManagement.Shared.csproj" `
    "$b\src\FuelManagement.Closures\FuelManagement.Closures.csproj" `
    "$b\src\FuelManagement.Alerts\FuelManagement.Alerts.csproj" `
    "$b\src\FuelManagement.Reports\FuelManagement.Reports.csproj"

# ---------------------------------------------------------------------------
# [4/7] Project references
# ---------------------------------------------------------------------------
Write-Host "`n[4/7] Project references..." -ForegroundColor Yellow
dotnet add "$b\src\FuelManagement.Closures\FuelManagement.Closures.csproj" `
    reference "$b\src\FuelManagement.Shared\FuelManagement.Shared.csproj"
dotnet add "$b\src\FuelManagement.Alerts\FuelManagement.Alerts.csproj" `
    reference "$b\src\FuelManagement.Shared\FuelManagement.Shared.csproj"
dotnet add "$b\src\FuelManagement.Reports\FuelManagement.Reports.csproj" `
    reference "$b\src\FuelManagement.Shared\FuelManagement.Shared.csproj"
dotnet add "$b\src\FuelManagement.Api\FuelManagement.Api.csproj" `
    reference "$b\src\FuelManagement.Shared\FuelManagement.Shared.csproj"
dotnet add "$b\src\FuelManagement.Api\FuelManagement.Api.csproj" `
    reference "$b\src\FuelManagement.Closures\FuelManagement.Closures.csproj"
dotnet add "$b\src\FuelManagement.Api\FuelManagement.Api.csproj" `
    reference "$b\src\FuelManagement.Alerts\FuelManagement.Alerts.csproj"
dotnet add "$b\src\FuelManagement.Api\FuelManagement.Api.csproj" `
    reference "$b\src\FuelManagement.Reports\FuelManagement.Reports.csproj"

# ---------------------------------------------------------------------------
# [5/7] NuGet packages
# ---------------------------------------------------------------------------
Write-Host "`n[5/7] NuGet packages..." -ForegroundColor Yellow

# API
dotnet add "$b\src\FuelManagement.Api\FuelManagement.Api.csproj" `
    package Swashbuckle.AspNetCore
dotnet add "$b\src\FuelManagement.Api\FuelManagement.Api.csproj" `
    package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add "$b\src\FuelManagement.Api\FuelManagement.Api.csproj" `
    package EFCore.NamingConventions

# Shared (DbContext lives here)
dotnet add "$b\src\FuelManagement.Shared\FuelManagement.Shared.csproj" `
    package Npgsql.EntityFrameworkCore.PostgreSQL

# Modules (service layer uses EF Core via DbContext)
dotnet add "$b\src\FuelManagement.Closures\FuelManagement.Closures.csproj" `
    package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add "$b\src\FuelManagement.Alerts\FuelManagement.Alerts.csproj" `
    package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add "$b\src\FuelManagement.Reports\FuelManagement.Reports.csproj" `
    package Npgsql.EntityFrameworkCore.PostgreSQL

# ---------------------------------------------------------------------------
# [6/7] Add FrameworkReference (ASP.NET Core) to classlib modules
#        Required for controllers, IHttpContextAccessor, etc. in non-Web projects
# ---------------------------------------------------------------------------
Write-Host "`n[6/7] Adding FrameworkReference to module projects..." -ForegroundColor Yellow

$moduleProjects = @(
    "$b\src\FuelManagement.Shared\FuelManagement.Shared.csproj",
    "$b\src\FuelManagement.Closures\FuelManagement.Closures.csproj",
    "$b\src\FuelManagement.Alerts\FuelManagement.Alerts.csproj",
    "$b\src\FuelManagement.Reports\FuelManagement.Reports.csproj"
)

foreach ($proj in $moduleProjects) {
    [xml]$xml = Get-Content $proj
    $itemGroup = $xml.CreateElement("ItemGroup")
    $fr = $xml.CreateElement("FrameworkReference")
    $fr.SetAttribute("Include", "Microsoft.AspNetCore.App")
    $itemGroup.AppendChild($fr) | Out-Null
    $xml.Project.AppendChild($itemGroup) | Out-Null
    $xml.Save($proj)
    Write-Host "  [OK] FrameworkReference added: $(Split-Path -Leaf $proj)"
}

# ---------------------------------------------------------------------------
# [7/7] Remove template boilerplate
# ---------------------------------------------------------------------------
Write-Host "`n[7/7] Removing template boilerplate..." -ForegroundColor Yellow

@(
    "$b\src\FuelManagement.Api\WeatherForecast.cs",
    "$b\src\FuelManagement.Api\Controllers\WeatherForecastController.cs",
    "$b\src\FuelManagement.Shared\Class1.cs",
    "$b\src\FuelManagement.Closures\Class1.cs",
    "$b\src\FuelManagement.Alerts\Class1.cs",
    "$b\src\FuelManagement.Reports\Class1.cs"
) | ForEach-Object {
    if (Test-Path $_) {
        Remove-Item $_ -Force
        Write-Host "  removed: $(Split-Path -Leaf $_)"
    }
}

Write-Host "`n[DONE] Scaffold complete!" -ForegroundColor Green
Write-Host "   Next: write source files, then run: dotnet build FuelManagement.sln" -ForegroundColor Gray
