namespace AutoBlumFarmServer.Model
{
    public class TGAuthOutputModel
    {
        public string   token        { get; set; } = string.Empty;
        public DateTime expires      { get; set; }
        public string   languageCode { get; set; } = string.Empty;
    }
}
