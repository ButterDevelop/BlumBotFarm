namespace BlumBotFarm.Core.Models
{
    public class ConfigModel
    {
        public int   Id                                     { get; set; }
        public bool  EnablePlayingForTickets                { get; set; } = true;
        public bool  EnableExecutingTasks                   { get; set; } = false;
        public float ChanceForPlayingTicketsAndPlayingTasks { get; set; } = 0.6f;
    }
}
