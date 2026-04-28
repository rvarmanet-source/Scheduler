using Quartz;

namespace Scheduler.Jobs;

public class DailyJob : IJob
{
    public Task Execute(IJobExecutionContext context)
    {
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Daily job executed");
        return Task.CompletedTask;
    }
}
