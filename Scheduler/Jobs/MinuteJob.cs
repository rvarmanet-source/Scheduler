using Quartz;
using Scheduler.Services;

namespace Scheduler.Jobs;

public class MinuteJob : IJob
{
    private readonly IEmailService _emailService;

    public MinuteJob(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        Console.WriteLine($"[{timestamp}] Minute job executed");

        // Send notification email
        await _emailService.SendEmailAsync(
            "test@gmail.com",
            "Minute Job Notification",
            $"<h2>Minute Job Executed</h2><p>The minute job ran successfully at {timestamp}.</p>"
        );
    }
}
