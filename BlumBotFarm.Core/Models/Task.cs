namespace BlumBotFarm.Core.Models
{
    public class Task
    {
        public int      Id                 { get; set; }
        public int      AccountId          { get; set; }
        public string   TaskType           { get; set; } = string.Empty;
        public int      MinScheduleSeconds { get; set; }
        public int      MaxScheduleSeconds { get; set; }
        public DateTime NextRunTime        { get; set; }
    }
}
