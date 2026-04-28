using Quartz;
using Quartz.Spi;
using Scheduler.Jobs;
using Scheduler.Services;

namespace Scheduler;

public class JobFactory : IJobFactory
{
    private readonly IEmailService _emailService;

    public JobFactory(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
    {
        var jobType = bundle.JobDetail.JobType;

        if (jobType == typeof(HourlyJob))
        {
            return new HourlyJob(_emailService);
        }

        if (jobType == typeof(DailyJob))
        {
            return new DailyJob();
        }

        if (jobType == typeof(MinuteJob))
        {
            return new MinuteJob(_emailService);
        }

        throw new NotSupportedException($"Job type {jobType.Name} is not supported");
    }

    public void ReturnJob(IJob job)
    {
        // No cleanup needed for this simple implementation
    }
}
