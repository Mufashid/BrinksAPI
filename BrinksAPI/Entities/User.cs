using System.ComponentModel.DataAnnotations;

namespace BrinksAPI.Entities
{
    public class User
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string? FirstName { get; set; }
        [Required]
        public string? LastName { get; set; }
        [Required]
        public string? Email { get; set; }
        [Required]
        public string? Password { get; set; }
        [Required]
        public bool isActive { get; set; }

        [Required]
        public int AuthLevelRefId { get; set; }

        [Required]
        public DateTime CreatedTime { get; set; }
        [Required]
        public DateTime UpdatedTime { get; set; }

    }
}
