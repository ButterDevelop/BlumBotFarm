using BlumBotFarm.Scheduler.Jobs;
using Quartz;
using Quartz.Impl;
using Quartz.Impl.Matchers;
using Task = System.Threading.Tasks.Task;

namespace BlumBotFarm.Scheduler
{
    public class TaskScheduler
    {
        public const int MIN_MS_AMOUNT_TO_WAIT_BEFORE_JOB = 30 * 1000, MAX_MS_AMOUNT_TO_WAIT_BEFORE_JOB = 30 * 60 * 1000;

        private readonly IScheduler scheduler;

        public TaskScheduler()
        {
            ISchedulerFactory schedulerFactory = new StdSchedulerFactory();
            scheduler = schedulerFactory.GetScheduler().Result;
            scheduler.Start().Wait();
        }

        public async Task<bool> DeleteAllTasks()
        {
            var jobGroups = await scheduler.GetJobGroupNames();

            foreach (string group in jobGroups)
            {
                var groupMatcher = GroupMatcher<JobKey>.GroupContains(group);
                var jobKeys      = await scheduler.GetJobKeys(groupMatcher);

                if (!await scheduler.DeleteJobs(jobKeys)) return false;
            }

            return true;
        }

        public async Task ScheduleTask(string jobName, string triggerName, IJobDetail jobDetail, ITrigger trigger)
        {
            await scheduler.ScheduleJob(jobDetail, trigger);
        }

        public static async Task ScheduleNewTask(TaskScheduler taskScheduler, int accountId, Core.Models.Task task, DateTime startAt, 
                                                 bool rightNow = false, bool isPlanned = true)
        {
            IJobDetail job = JobBuilder.Create<DailyCheckJob>().Build();
            job.JobDataMap.Put("accountId", accountId);
            job.JobDataMap.Put("taskId" + task.TaskType, task.Id);
            job.JobDataMap.Put("isPlanned", isPlanned);

            ITrigger? trigger;
            if (rightNow)
            {
                trigger = TriggerBuilder.Create()
                    .WithSimpleSchedule(schedule => schedule.WithRepeatCount(0))
                    .StartNow()
                    .Build();
            }
            else
            {
                trigger = TriggerBuilder.Create()
                    .WithSimpleSchedule(schedule => schedule.WithRepeatCount(0))
                    .StartAt(startAt)
                    .Build();
            }

            await taskScheduler.ScheduleTask(task.Id.ToString(), task.Id.ToString(), job, trigger);
        }

        public static async Task ExecuteMainJobNow()
        {
            // Создание экземпляра планировщика задач
            var taskScheduler = new TaskScheduler();

            // Создание задачи для MainSchedulerJob
            IJobDetail job = JobBuilder.Create<MainSchedulerJob>().Build();

            var triggerImmediately = TriggerBuilder.Create()
                                .StartNow()
                                .Build();

            await taskScheduler.ScheduleTask("MainSchedulerJobImmediately", "MainSchedulerJobTriggerImmediately", job, triggerImmediately);
        }

        public static async Task ScheduleMainJob()
        {
            // Создание экземпляра планировщика задач
            var taskScheduler = new TaskScheduler();

            // Создание задачи для MainSchedulerJob
            IJobDetail job = JobBuilder.Create<MainSchedulerJob>().Build();

            var triggerScheduled = TriggerBuilder.Create()
                                .WithSchedule(CronScheduleBuilder.DailyAtHourAndMinute(0, 1))
                                .Build();

            await taskScheduler.ScheduleTask("MainSchedulerJobScheduled", "MainSchedulerJobTriggerScheduled", job, triggerScheduled);
        }

        public static async Task ScheduleEarningJob(TaskScheduler taskScheduler, int accountId, double balance, string type, DateTime startAt)
        {
            IJobDetail job = JobBuilder.Create<EarningCheckJob>().Build();
            job.JobDataMap.Put("accountId", accountId);
            job.JobDataMap.Put("balance", balance);
            job.JobDataMap.Put("type", type);

            var trigger = TriggerBuilder.Create()
                    .WithSimpleSchedule(schedule => schedule.WithRepeatCount(0))
                    .StartAt(startAt)
                    .Build();

            await taskScheduler.ScheduleTask($"EarningJob_{accountId}", $"EarningJob_{accountId}", job, trigger);
        }

        public static async Task UpdateUsersInfoNow()
        {
            // Создание экземпляра планировщика задач
            var taskScheduler = new TaskScheduler();

            // Создание задачи для UpdateUsersInfoJob
            IJobDetail job = JobBuilder.Create<UpdateUsersInfoJob>().Build();

            var triggerImmediately = TriggerBuilder.Create().StartNow().Build();

            await taskScheduler.ScheduleTask("UpdateUsersInfoImmediately", "UpdateUsersInfoImmediately", job, triggerImmediately);
        }

        public static async Task ScheduleUpdateUsersInfo()
        {
            // Создание экземпляра планировщика задач
            var taskScheduler = new TaskScheduler();

            // Создание задачи для MainSchedulerJob
            IJobDetail job = JobBuilder.Create<UpdateUsersInfoJob>().Build();

            Random random    = new();
            int minutesToAdd = random.Next(UpdateUsersInfoJob.MIN_MINUTES_TO_WAIT, UpdateUsersInfoJob.MAX_MINUTES_TO_WAIT);
            DateTime startAt = DateTime.Now.AddMinutes(minutesToAdd);

            var triggerScheduled = TriggerBuilder.Create()
                                .WithSimpleSchedule(schedule => schedule.WithRepeatCount(0))
                                .StartAt(startAt)
                                .Build();

            await taskScheduler.ScheduleTask("ScheduleUpdateUsersInfo", "ScheduleUpdateUsersInfo", job, triggerScheduled);
        }
    }
}
