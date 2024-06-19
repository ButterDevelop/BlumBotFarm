using Quartz;
using Quartz.Impl;

namespace BlumBotFarm.Scheduler
{
    public class TaskScheduler
    {
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
