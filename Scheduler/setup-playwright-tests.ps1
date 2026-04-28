#!/usr/bin/env pwsh

# Scheduler Playwright Test Project Setup Script
# This script automates the creation of a complete Playwright test project for the Scheduler application

param(
    [string]$ProjectPath = "C:\Projects\Scheduler\Scheduler"
)

Write-Host "🚀 Starting Scheduler Playwright Test Project Setup" -ForegroundColor Cyan
Write-Host "Project Path: $ProjectPath" -ForegroundColor Gray

# Check if project path exists
if (-not (Test-Path $ProjectPath)) {
    Write-Host "❌ Project path not found: $ProjectPath" -ForegroundColor Red
    exit 1
}

# Navigate to project directory
Push-Location $ProjectPath

try {
    # Step 1: Create test project
    Write-Host "`n📦 Creating xUnit test project..." -ForegroundColor Green
    dotnet new xunit -n Scheduler.Tests -f net10.0 -force
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Failed to create test project" -ForegroundColor Red
        exit 1
    }

    # Step 2: Add NuGet packages
    Write-Host "`n📥 Adding NuGet packages..." -ForegroundColor Green
    $packages = @(
        "Microsoft.Playwright@1.45.0",
        "Playwright@1.45.0",
        "xunit@2.7.0",
        "xunit.runner.visualstudio@2.5.0"
    )

    foreach ($package in $packages) {
        Write-Host "  Adding $package..." -ForegroundColor Gray
        dotnet add Scheduler.Tests package $package
    }

    # Step 3: Create directory structure
    Write-Host "`n📁 Creating test project structure..." -ForegroundColor Green
    $testProjectPath = Join-Path $ProjectPath "Scheduler.Tests"

    @(
        "Fixtures",
        "Tests",
        "Mocks"
    ) | ForEach-Object {
        $dir = Join-Path $testProjectPath $_
        if (-not (Test-Path $dir)) {
            New-Item -ItemType Directory -Path $dir | Out-Null
            Write-Host "  ✓ Created: $_" -ForegroundColor Gray
        }
    }

    # Step 4: Create fixture files
    Write-Host "`n🔧 Creating test fixtures..." -ForegroundColor Green

    # PlaywrightFixture.cs
    $playWrightFixture = @'
using Microsoft.Playwright;

namespace Scheduler.Tests.Fixtures;

public class PlaywrightFixture : IAsyncLifetime
{
    private IPlaywright? _playwright;
    public IBrowser? Browser { get; private set; }
    public IBrowserContext? BrowserContext { get; private set; }
    public IPage? Page { get; private set; }

    public async Task InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        Browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });
        BrowserContext = await Browser.NewContextAsync();
        Page = await BrowserContext.NewPageAsync();
    }

    public async Task DisposeAsync()
    {
        if (Page != null) await Page.CloseAsync();
        if (BrowserContext != null) await BrowserContext.CloseAsync();
        if (Browser != null) await Browser.CloseAsync();
        _playwright?.Dispose();
    }
}
'@
    Set-Content -Path (Join-Path $testProjectPath "Fixtures\PlaywrightFixture.cs") -Value $playWrightFixture
    Write-Host "  ✓ Created: PlaywrightFixture.cs" -ForegroundColor Gray

    # Step 5: Create mock services
    Write-Host "`n🎭 Creating mock services..." -ForegroundColor Green

    # MockEmailService.cs
    $mockEmailService = @'
using Scheduler.Services;

namespace Scheduler.Tests.Mocks;

public class MockEmailService : IEmailService
{
    public int EmailsSent { get; private set; }
    public string? LastEmailSubject { get; private set; }
    public string? LastEmailBody { get; private set; }
    public string? LastEmailRecipient { get; private set; }

    public Task SendEmailAsync(string toEmail, string subject, string body)
    {
        EmailsSent++;
        LastEmailSubject = subject;
        LastEmailBody = body;
        LastEmailRecipient = toEmail;
        return Task.CompletedTask;
    }
}
'@
    Set-Content -Path (Join-Path $testProjectPath "Mocks\MockEmailService.cs") -Value $mockEmailService
    Write-Host "  ✓ Created: MockEmailService.cs" -ForegroundColor Gray

    # Step 6: Build project
    Write-Host "`n🔨 Building test project..." -ForegroundColor Green
    Set-Location (Join-Path $ProjectPath "Scheduler.Tests")
    dotnet build

    # Step 7: Install Playwright browsers
    Write-Host "`n🌐 Installing Playwright browsers..." -ForegroundColor Green
    & pwsh bin/Debug/net10.0/playwright.ps1 install

    Write-Host "`n✅ Setup completed successfully!" -ForegroundColor Green
    Write-Host "`nNext steps:" -ForegroundColor Cyan
    Write-Host "1. Add more test files to the Tests/ directory" -ForegroundColor Gray
    Write-Host "2. Run tests with: dotnet test" -ForegroundColor Gray
    Write-Host "3. View test results in Test Explorer" -ForegroundColor Gray
    Write-Host "`nDocumentation: See PLAYWRIGHT_TESTING_GUIDE.md for examples and best practices" -ForegroundColor Cyan

}
catch {
    Write-Host "❌ Error: $_" -ForegroundColor Red
    exit 1
}
finally {
    Pop-Location
}
