using System.ComponentModel.DataAnnotations;

namespace BrinksAPI.Entities
{
    public class PackageType
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string? BrinksCode { get; set; }
        [Required]
        public string? CWCode { get; set; }
    }
}
