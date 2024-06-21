using Quartz;
using Quartz.Impl;

namespace BlumBotFarm.Scheduler
{
    public class TaskScheduler
    {
        public const int MIN_MS_AMOUNT_TO_WAIT_BEFORE_JOB = 30 * 1000, MAX_MS_AMOUNT_TO_WAIT_BEFORE_JOB = 3 * 60 * 1000;

        private readonly IScheduler scheduler;

        public TaskScheduler()
        {
            ISchedulerFactory schedulerFactory = new StdSchedulerFactory();
            scheduler = schedulerFactory.GetScheduler().Result;
            scheduler.Start().Wait();
        }

        public async Task ScheduleTask(string jobName, string triggerName, IJobDetail jobDetail, ITrigger trigger)
        {
            await scheduler.ScheduleJob(jobDetail, trigger);
        }
    }
}
