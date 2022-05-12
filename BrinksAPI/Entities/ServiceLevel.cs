using System.ComponentModel.DataAnnotations;

namespace BrinksAPI.Entities
{
    public class ServiceLevel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(10)]
        public string? CWCode { get; set; }

        [Required]
        [MaxLength(10)]
        public string? BrinksCode { get; set; }

        [MaxLength(30)]
        public string? Description { get; set; }
    }
}
