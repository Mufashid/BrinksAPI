namespace BrinksAPI.Entities
{
    public class Site
    {
        public int ServerID { get; set; }
        public int SiteCode { get; set; }
        public string? CountryCode { get; set; }
        public string? CityName { get; set; }
    }
}
