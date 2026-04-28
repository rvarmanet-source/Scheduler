using Quartz;
using Quartz.Impl;
using Scheduler;
using Scheduler.Jobs;
using Scheduler.Services;

// Configure email service (update with your SMTP settings)
var emailService = new EmailService(
    smtpServer: "smtp.gmail.com",      // e.g., smtp.gmail.com, smtp.office365.com
    smtpPort: 587,                      // Usually 587 for StartTls or 465 for SSL
    emailAddress: "your-email@gmail.com",
    emailPassword: "your-app-password"  // Use app-specific password for Gmail
);

// Create scheduler factory with dependency injection
var schedulerFactory = new StdSchedulerFactory();
var scheduler = await schedulerFactory.GetScheduler();

// Configure job factory to support dependency injection
scheduler.JobFactory = new JobFactory(emailService);

// Start the scheduler
await scheduler.Start();

// Define and schedule the hourly job
var hourlyJob = JobBuilder.Create<HourlyJob>()
    .WithIdentity("hourlyJob", "group1")
    .Build();

var hourlyTrigger = TriggerBuilder.Create()
    .WithIdentity("hourlyTrigger", "group1")
    .WithSchedule(SimpleScheduleBuilder.RepeatHourlyForever())
    .Build();

await scheduler.ScheduleJob(hourlyJob, hourlyTrigger);

// Define and schedule the minute job (runs every minute using cron expression)
var minuteJob = JobBuilder.Create<MinuteJob>()
    .WithIdentity("minuteJob", "group1")
    .Build();

var minuteTrigger = TriggerBuilder.Create()
    .WithIdentity("minuteTrigger", "group1")
    .WithSchedule(CronScheduleBuilder.CronSchedule("0 * * * * ?"))  // Runs every minute at second 0
    .Build();

await scheduler.ScheduleJob(minuteJob, minuteTrigger);

// Define and schedule the daily job (runs at 2 AM every day)
var dailyJob = JobBuilder.Create<DailyJob>()
    .WithIdentity("dailyJob", "group1")
    .Build();

var dailyTrigger = TriggerBuilder.Create()
    .WithIdentity("dailyTrigger", "group1")
    .WithSchedule(CronScheduleBuilder.DailyAtHourAndMinute(2, 0))
    .Build();

await scheduler.ScheduleJob(dailyJob, dailyTrigger);

Console.WriteLine("Scheduler started. Press any key to shutdown.");
Console.ReadKey();

// Shutdown the scheduler
await scheduler.Shutdown();
