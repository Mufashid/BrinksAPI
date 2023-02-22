using System.ComponentModel.DataAnnotations;

namespace BrinksAPI.Entities
{
    public class Site
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public int ServerID { get; set; }
        [Required]
        public int SiteCode { get; set; }
        public string? FinancialMgmt { get; set; }
        public string? Description { get; set; }
        public string? ExtendedDescription { get; set; }
        public string? Airport { get; set; }
        public string? Country { get; set; }
        public string? Province { get; set; }
        public string? PostalCode { get; set; }
        public string? City { get; set; }
        public string? CityCode { get; set; }
        public string? Address1 { get; set; }
        public string? Address2 { get; set; }
        public string? Address3 { get; set; }
        public string? Address4 { get; set; }
        public string? EmailID { get; set; }
        public string? PhoneNumber { get; set; }
        public string? FaxNumber { get; set; }
        [Required]
        public string? CompanyCode { get; set; }
        public string? BranchCode { get; set; }

    }
}
