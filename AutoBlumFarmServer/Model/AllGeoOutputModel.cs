namespace AutoBlumFarmServer.Model
{
    public class AllGeoOutputModel
    {
        public List<GeoEntry> Geos { get; set; } = [];
    }

    public class GeoEntry
    {
        public string CountryCode    { get; set; } = string.Empty;
        public string CountryName    { get; set; } = string.Empty;
        public int    TimezoneOffset { get; set; }
    }
}
