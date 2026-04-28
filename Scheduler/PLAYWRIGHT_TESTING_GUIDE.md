# Playwright Testing Setup Guide for Scheduler

This guide walks you through setting up and running Playwright tests for your Quartz Scheduler application in .NET 10.

## Prerequisites

- .NET 10 SDK
- Visual Studio 2026 or later
- Node.js (for Playwright browser binaries)

## Setup Steps

### 1. Create a New Test Project

```powershell
cd C:\Projects\Scheduler\Scheduler
dotnet new xunit -n Scheduler.Tests -f net10.0
```

### 2. Add Playwright NuGet Package

```powershell
cd Scheduler.Tests
dotnet add package Microsoft.Playwright --version 1.45.0
dotnet add package Playwright --version 1.45.0
dotnet add package xunit --version 2.7.0
dotnet add package xunit.runner.visualstudio --version 2.5.0
```

### 3. Install Playwright Browsers

```powershell
pwsh bin/Debug/net10.0/playwright.ps1 install
# Or using global installation:
npm install -g @playwright/test
playwright install
```

## Test Project Structure

```
Scheduler.Tests/
├── Fixtures/
│   ├── PlaywrightFixture.cs          # Base fixture for browser initialization
│   └── ConsoleApplicationFixture.cs   # Fixture for console app testing
├── Tests/
│   ├── ConsoleApplicationTests.cs     # Console app process tests
│   ├── PlaywrightTests.cs             # Browser automation tests
│   └── JobExecutionTests.cs           # Job unit tests
└── Mocks/
    ├── MockEmailService.cs            # Mock email service
    └── MockJobExecutionContext.cs      # Mock Quartz job context
```

## Example Test Files

### PlaywrightFixture.cs
```csharp
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
            Headless = true,
            Args = new[] { "--disable-blink-features=AutomationControlled" }
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
```

### ConsoleApplicationTests.cs
```csharp
using System.Diagnostics;
using Xunit;

namespace Scheduler.Tests.Tests;

public class ConsoleApplicationTests
{
    [Fact]
    public void SchedulerApplicationStartsSuccessfully()
    {
        // Arrange
        var projectPath = Path.Combine(
            GetProjectRoot(), 
            "bin", "Debug", "net10.0", "Scheduler.exe"
        );
        Assert.True(File.Exists(projectPath), "Scheduler executable not found");

        // Act
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = projectPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            }
        };

        process.Start();
        System.Threading.Thread.Sleep(3000);

        // Assert
        Assert.False(process.HasExited, "Scheduler process exited prematurely");
        process.Kill();
    }

    private string GetProjectRoot()
    {
        var currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
        return Path.GetFullPath(Path.Combine(currentDirectory, "..", "..", "..", ".."));
    }
}
```

### PlaywrightTests.cs
```csharp
using Microsoft.Playwright;
using Scheduler.Tests.Fixtures;
using Xunit;

namespace Scheduler.Tests.Tests;

public class PlaywrightTests : IAsyncLifetime
{
    private PlaywrightFixture _fixture = new();

    public Task InitializeAsync() => _fixture.InitializeAsync();
    public Task DisposeAsync() => _fixture.DisposeAsync();

    [Fact]
    public async Task CanLaunchBrowser()
    {
        Assert.NotNull(_fixture.Browser);
        Assert.NotNull(_fixture.Page);

        var version = _fixture.Browser!.Version;
        Assert.NotEmpty(version);
    }

    [Fact]
    public async Task CanNavigateAndCapture()
    {
        // Navigate to a page
        await _fixture.Page!.GotoAsync("https://www.example.com");

        // Capture screenshot
        var screenshotPath = Path.Combine(Path.GetTempPath(), "test_screenshot.png");
        await _fixture.Page.ScreenshotAsync(new() { Path = screenshotPath });

        Assert.True(File.Exists(screenshotPath));
        Assert.True(new FileInfo(screenshotPath).Length > 0);

        // Cleanup
        File.Delete(screenshotPath);
    }
}
```

### JobExecutionTests.cs
```csharp
using Scheduler.Jobs;
using Scheduler.Services;
using Scheduler.Tests.Mocks;
using Xunit;

namespace Scheduler.Tests.Tests;

public class JobExecutionTests
{
    [Fact]
    public async Task HourlyJobSendsEmailNotification()
    {
        // Arrange
        var mockEmailService = new MockEmailService();
        var job = new HourlyJob(mockEmailService);
        var context = new MockJobExecutionContext();

        // Act
        await job.Execute(context);

        // Assert
        Assert.Equal(1, mockEmailService.EmailsSent);
        Assert.Contains("Hourly", mockEmailService.LastEmailSubject);
    }

    [Fact]
    public async Task MinuteJobSendsEmailNotification()
    {
        // Arrange
        var mockEmailService = new MockEmailService();
        var job = new MinuteJob(mockEmailService);
        var context = new MockJobExecutionContext();

        // Act
        await job.Execute(context);

        // Assert
        Assert.Equal(1, mockEmailService.EmailsSent);
        Assert.Contains("Minute", mockEmailService.LastEmailSubject);
    }

    [Fact]
    public async Task MultipleJobsTrackEmails()
    {
        // Arrange
        var mockEmailService = new MockEmailService();
        var hourlyJob = new HourlyJob(mockEmailService);
        var minuteJob = new MinuteJob(mockEmailService);
        var context = new MockJobExecutionContext();

        // Act
        await hourlyJob.Execute(context);
        await minuteJob.Execute(context);

        // Assert
        Assert.Equal(2, mockEmailService.EmailsSent);
    }
}
```

### Mocks/MockEmailService.cs
```csharp
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
```

### Mocks/MockJobExecutionContext.cs
```csharp
using Quartz;

namespace Scheduler.Tests.Mocks;

public class MockJobExecutionContext : IJobExecutionContext
{
    public IScheduler Scheduler => throw new NotImplementedException();
    public ITrigger Trigger => throw new NotImplementedException();
    public IJobDetail JobDetail => throw new NotImplementedException();
    public object? Result { get; set; }
    public DateTimeOffset FireTimeUtc => DateTimeOffset.UtcNow;
    public DateTimeOffset? ScheduledFireTimeUtc => null;
    public DateTimeOffset? PreviousFireTimeUtc => null;
    public DateTimeOffset? NextFireTimeUtc => null;
    public int RefireCount => 0;
    public JobExecutionException? JobException { get; set; }
    public string FireInstanceId => Guid.NewGuid().ToString();
    public TimeSpan JobRunTime => TimeSpan.Zero;
    public JobDataMap MergedJobDataMap => new();
    public IReadOnlyDictionary<object, object?> MergedJobDataMapAsMap => MergedJobDataMap;
    public ICalendar? Calendar => null;
    public TriggerKey? RecoveringTriggerKey => null;
    public IJob? JobInstance => null;
    public CancellationToken CancellationToken => CancellationToken.None;

    public void SetResult(object? resultObject) { }
    public void Put(object key, object? value) { }
    public object? Get(object key) => null;
}
```

## Running Tests

### From Command Line

```powershell
# Run all tests
dotnet test

# Run specific test class
dotnet test --filter "ClassName=ConsoleApplicationTests"

# Run with verbose output
dotnet test -- -verbose

# Run with code coverage
dotnet test /p:CollectCoverage=true /p:CoverageFormat=opencover
```

### From Visual Studio

1. Open Test Explorer (`Test > Test Explorer`)
2. Click "Run All Tests" or run specific tests
3. View results and debug failures

### With Playwright Inspector

```powershell
# Debug tests interactively
$env:PWDEBUG=1
dotnet test
```

## Common Issues and Solutions

### Issue: Playwright browsers not found
**Solution:**
```powershell
# Install browsers globally
pwsh bin/Debug/net10.0/playwright.ps1 install
# Or reinstall packages
dotnet add package Playwright --version latest
```

### Issue: Tests timeout
**Solution:**
- Increase timeout in test attributes
- Check if Chromium process is hung: `Get-Process | Where-Object {$_.ProcessName -like "*chrome*"} | Stop-Process`

### Issue: SMTP errors in email tests
**Solution:**
- Mock email service is already implemented
- Use `MockEmailService` for unit tests
- Never use real credentials in tests

##  CI/CD Integration

### GitHub Actions Example
```yaml
name: Test

on: [push, pull_request]

jobs:
  test:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '10.0.x'
      - run: dotnet restore
      - run: dotnet build
      - run: dotnet test --no-build --logger "console;verbosity=detailed"
```

## Best Practices

1. **Use Fixtures**: Leverage xUnit fixtures for setup/teardown
2. **Mock External Services**: Always mock email, database, APIs
3. **Async/Await**: Use async tests for I/O operations
4. **Meaningful Names**: Name tests to describe what they verify
5. **Arrange-Act-Assert**: Follow AAA pattern in tests
6. **Keep Tests Fast**: Avoid lengthy waits; use proper timeouts
7. **Isolate Tests**: Each test should be independent

## Additional Resources

- [Microsoft.Playwright Documentation](https://playwright.dev/dotnet/)
- [xUnit.net Documentation](https://xunit.net/)
- [Quartz.NET Documentation](https://www.quartz-scheduler.net/)
- [MailKit Documentation](https://github.com/jstedfast/MailKit)

## Next Steps

1. Create the test project using the commands above
2. Copy the example test classes into your project
3. Run `dotnet test` to execute tests
4. Add more tests as you develop new features
5. Integrate tests into your CI/CD pipeline

---

For more information or issues, refer to the official documentation links above.
