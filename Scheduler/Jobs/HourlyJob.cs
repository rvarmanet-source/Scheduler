using Quartz;
using Scheduler.Services;

namespace Scheduler.Jobs;

public class HourlyJob : IJob
{
    private readonly IEmailService _emailService;

    public HourlyJob(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        Console.WriteLine($"[{timestamp}] Hourly job executed");

        // Send notification email
        await _emailService.SendEmailAsync(
            "test@gmail.com",
            "Hourly Job Notification",
            $"<h2>Hourly Job Executed</h2><p>The hourly job ran successfully at {timestamp}.</p>"
        );
    }
}
