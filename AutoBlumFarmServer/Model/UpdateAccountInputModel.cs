namespace AutoBlumFarmServer.Model
{
    public class UpdateAccountInputModel
    {
        public string CustomUsername   { get; set; } = string.Empty;
        public string CountryCode      { get; set; } = string.Empty;
        public string BlumTelegramAuth { get; set; } = string.Empty;
    }
}
