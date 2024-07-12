namespace BlumBotFarm.Core.Models
{
    public class Referral
    {
        public int Id              { get; set; }
        public int HostUserId      { get; set; }
        public int DependentUserId { get; set; }
    }
}
