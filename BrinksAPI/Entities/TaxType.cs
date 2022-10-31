using System.ComponentModel.DataAnnotations;

namespace BrinksAPI.Entities
{
    public class TaxType
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string? BrinksCode { get; set; }
        [Required]
        public string? CWCode { get; set; }
        public string? ActualValue { get; set; }
    }
}
