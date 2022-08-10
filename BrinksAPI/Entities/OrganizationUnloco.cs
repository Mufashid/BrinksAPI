using System.ComponentModel.DataAnnotations;

namespace BrinksAPI.Entities
{
    public class OrganizationUnloco
    {
        public string? Country { get; set; }
        public string? Alpha2Code { get; set; }
        public string? Alpha3Code { get; set; }
        public string? Numeric { get; set; }
        [Key]
        public string? DefaultUNLOCO { get; set; }
    }
}
