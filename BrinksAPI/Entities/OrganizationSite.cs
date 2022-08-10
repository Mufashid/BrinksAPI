using System.ComponentModel.DataAnnotations;

namespace BrinksAPI.Entities
{
    public class OrganizationSite
    {
        public string? CountryCode { get; set; }
        public string? Bits { get; set; }
        [Key]
        public string? SiteCode { get; set; }
        public string? CWBranchCode { get; set; }
        public string? Unloco { get; set; }
        public string? OperationalMgmt { get; set; }
        public string? FinancialMgmt { get; set; }
        public string? Description { get; set; }
        public string? ExtendedDescription { get; set; }
        public string? ProvinceCode { get; set; }
        public string? PostalCode { get; set; }
        public string? City { get; set; }
        public string? CityCode { get; set; }
        public string? Address1 { get; set; }
        public string? Address2 { get; set; }
        public string? Address3 { get; set; }
        public string? Address4 { get; set; }
        public string? EmailAddress { get; set; }
        public string? PhoneNumber { get; set; }
        public string? FaxNumber { get; set; }
    }
}
